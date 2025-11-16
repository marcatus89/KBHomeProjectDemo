// Services/OrderService.cs
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace DoAnTotNghiep.Services
{
    public record OrderResult(bool Success, int? OrderId = null, string? ErrorMessage = null);

    public class OrderService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<OrderService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Tạo đơn: thực hiện kiểm tra tồn và giảm tồn **atomically** bằng câu lệnh UPDATE trên DB.
        /// Tất cả trong transaction.
        /// </summary>
        public async Task<OrderResult> CreateOrderAsync(string? userId,
                                                        string customerName,
                                                        string phoneNumber,
                                                        string shippingAddress,
                                                        IEnumerable<CartItem> cartItems,
                                                        decimal totalAmount)
        {
            var items = (cartItems ?? Enumerable.Empty<CartItem>()).ToList();
            _logger.LogInformation("CreateOrderAsync START. UserId={UserId}, Customer={Customer}, Total={Total}, Items={Count}",
                userId, customerName, totalAmount, items.Count);

            if (!items.Any())
            {
                return new OrderResult(false, null, "Giỏ hàng trống.");
            }

            // Group by productId to compute totals per product
            var qtyByProduct = items
                .GroupBy(ci => ci.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                // Load product basic info (name, price) to validate existence
                var productIds = qtyByProduct.Keys.ToArray();
                var products = await db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                // Check for missing products
                var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
                if (missing.Any())
                {
                    var miss = string.Join(",", missing);
                    _logger.LogWarning("Missing products in DB: {Missing}", miss);
                    await tx.RollbackAsync();
                    return new OrderResult(false, null, $"Không tìm thấy sản phẩm: {miss}");
                }

                // Create order first so we have order.Id for logs
                var order = new Order
                {
                    CustomerName = customerName,
                    PhoneNumber = phoneNumber,
                    ShippingAddress = shippingAddress,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = "Chờ xác nhận",
                    UserId = userId
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync(); // order.Id assigned

                _logger.LogInformation("Created Order Id={OrderId}", order.Id);

                // Resolve friendly placedBy (email/username/customerName) if possible
                string placedBy = !string.IsNullOrWhiteSpace(customerName) ? customerName : "Khách lẻ";
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    try
                    {
                        var identityUser = await db.Set<IdentityUser>().FindAsync(userId);
                        if (identityUser != null)
                        {
                            if (!string.IsNullOrWhiteSpace(identityUser.Email))
                                placedBy = identityUser.Email;
                            else if (!string.IsNullOrWhiteSpace(identityUser.UserName))
                                placedBy = identityUser.UserName;
                            else
                                placedBy = userId;
                        }
                        else
                        {
                            placedBy = !string.IsNullOrWhiteSpace(customerName) ? customerName : userId;
                        }
                    }
                    catch
                    {
                        placedBy = !string.IsNullOrWhiteSpace(customerName) ? customerName : userId;
                    }
                }

                // 1) For each product, attempt atomic decrement via UPDATE ... WHERE StockQuantity >= required
                // 2) If any affected == 0 => insufficient stock -> rollback
                // 3) After successful decrements, create OrderDetails (per original cart lines) and one InventoryLog per product

                var inventoryLogs = new List<InventoryLog>();

                foreach (var kv in qtyByProduct)
                {
                    var pid = kv.Key;
                    var required = kv.Value;
                    var productInfo = products[pid];

                    // Atomic update: reduce stock only if enough
                    var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE Products
                        SET StockQuantity = StockQuantity - {required}
                        WHERE Id = {pid} AND StockQuantity >= {required}
                    ");

                    if (affected == 0)
                    {
                        // Not enough stock
                        // Fetch current stock to include in message if possible
                        var currentStock = await db.Products.AsNoTracking().Where(p => p.Id == pid).Select(p => p.StockQuantity).FirstOrDefaultAsync();
                        _logger.LogWarning("Not enough stock for ProductId={Pid}. Available={Stock}, Required={Req}. Rolling back.", pid, currentStock, required);
                        await tx.RollbackAsync();
                        return new OrderResult(false, null, $"Sản phẩm \"{productInfo.Name}\" chỉ còn {currentStock} trong kho.");
                    }

                    // Read new stock after update
                    var newQty = await db.Products.AsNoTracking().Where(p => p.Id == pid).Select(p => p.StockQuantity).FirstAsync();
                    var oldQty = newQty + required;

                    inventoryLogs.Add(new InventoryLog
                    {
                        ProductId = pid,
                        OldQuantity = oldQty,
                        QuantityChange = -required,
                        NewQuantity = newQty,
                        Reason = $"Bán hàng - Đơn #{order.Id} bởi {placedBy}",
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Product {Pid}: {Old} -> {New} (change {Change})", pid, oldQty, newQty, -required);
                }

                // Create OrderDetails for each cart line (keeps line-level info)
                foreach (var ci in items)
                {
                    var od = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = ci.ProductId,
                        ProductName = ci.ProductName,
                        Quantity = ci.Quantity,
                        Price = ci.Price
                    };
                    db.OrderDetails.Add(od);
                }

                // Add inventory logs (one per product)
                db.InventoryLogs.AddRange(inventoryLogs);

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("CreateOrderAsync COMMIT success OrderId={OrderId}", order.Id);
                return new OrderResult(true, order.Id, null);
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                _logger.LogError(ex, "CreateOrderAsync failed - rolled back");
                return new OrderResult(false, null, ex.Message);
            }
        }

        // Wrapper gọi CreateOrderAsync từ Checkout (giữ interface cũ).
        public Task<OrderResult> PlaceOrderAsync(Order orderModel,
                                                 ClaimsPrincipal? user,
                                                 IEnumerable<CartItem> cartItems)
        {
            string? userId = null;
            if (user?.Identity?.IsAuthenticated == true)
            {
                userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            var customerName = string.IsNullOrWhiteSpace(orderModel.CustomerName) ? (user?.Identity?.Name ?? "Khách lẻ") : orderModel.CustomerName;
            var phone = orderModel.PhoneNumber ?? "";
            var address = orderModel.ShippingAddress ?? "";
            var total = cartItems.Sum(ci => ci.Price * ci.Quantity);

            _logger.LogInformation("PlaceOrderAsync called by user {User}. Items={Count}, Total={Total}", userId ?? "(anon)", cartItems?.Count() ?? 0, total);
            return CreateOrderAsync(userId, customerName, phone, address, cartItems, total);
        }

        // Các overload khác giữ nguyên
        public Task<OrderResult> PlaceOrderAsync(ClaimsPrincipal? user,
                                                 string customerName,
                                                 string phoneNumber,
                                                 string shippingAddress,
                                                 IEnumerable<CartItem> cartItems,
                                                 decimal totalAmount)
        {
            string? userId = null;
            if (user?.Identity?.IsAuthenticated == true)
            {
                userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            return CreateOrderAsync(userId, customerName, phoneNumber, shippingAddress, cartItems, totalAmount);
        }

        public Task<OrderResult> PlaceOrderAsync(string? userId,
                                                 string customerName,
                                                 string phoneNumber,
                                                 string shippingAddress,
                                                 IEnumerable<CartItem> cartItems,
                                                 decimal totalAmount)
        {
            return CreateOrderAsync(userId, customerName, phoneNumber, shippingAddress, cartItems, totalAmount);
        }

        /// <summary>
        /// Hủy đơn: trả hàng về kho và ghi InventoryLog (có OldQuantity).
        /// </summary>
        public async Task<OrderResult> CancelOrderAsync(int orderId, ClaimsPrincipal? canceledBy = null)
        {
            _logger.LogInformation("CancelOrderAsync START OrderId={OrderId} by {User}", orderId, canceledBy?.Identity?.Name ?? "(unknown)");

            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var order = await db.Orders
                                    .Include(o => o.OrderDetails)
                                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return new OrderResult(false, null, "Không tìm thấy đơn hàng.");
                }

                if (order.Status == "Đã hủy")
                {
                    _logger.LogWarning("Order {OrderId} already cancelled", orderId);
                    return new OrderResult(false, null, "Đơn hàng đã bị hủy trước đó.");
                }

                var who = canceledBy?.Identity?.IsAuthenticated == true ? canceledBy.Identity.Name : "Hệ thống";

                foreach (var od in order.OrderDetails)
                {
                    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == od.ProductId);
                    if (product == null) continue;

                    int oldQty = product.StockQuantity;
                    product.StockQuantity += od.Quantity;
                    db.Products.Update(product);

                    var log = new InventoryLog
                    {
                        ProductId = product.Id,
                        OldQuantity = oldQty,
                        QuantityChange = od.Quantity,
                        NewQuantity = product.StockQuantity,
                        Reason = $"Hoàn trả đơn #{orderId} (hủy bởi {who})",
                        Timestamp = DateTime.UtcNow
                    };
                    db.InventoryLogs.Add(log);

                    _logger.LogInformation("Returned {Qty} to product {Pid}. NewStock={Stock}", od.Quantity, product.Id, product.StockQuantity);
                }

                order.Status = "Đã hủy";
                db.Orders.Update(order);

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("CancelOrderAsync COMMIT success OrderId={OrderId}", orderId);
                return new OrderResult(true, order.Id, null);
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                _logger.LogError(ex, "CancelOrderAsync failed");
                return new OrderResult(false, null, ex.Message);
            }
        }
    }
}
