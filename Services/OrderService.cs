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
        /// Tạo đơn (được gọi từ UI/Checkout). Thực hiện kiểm tra tồn trước, tạo order, trừ tồn và ghi InventoryLog (có OldQuantity).
        /// Tất cả diễn ra trong transaction.
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

            // Group by productId so we handle duplicate product lines correctly
            var qtyByProduct = items
                .GroupBy(ci => ci.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Load all products involved (single query)
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

                // Check stock availability for every product
                foreach (var kv in qtyByProduct)
                {
                    var pid = kv.Key;
                    var required = kv.Value;
                    var p = products[pid];
                    if (p.StockQuantity < required)
                    {
                        _logger.LogWarning("Not enough stock for ProductId={Pid}. Available={Stock}, Required={Req}. Rolling back.", pid, p.StockQuantity, required);
                        await tx.RollbackAsync();
                        return new OrderResult(false, null, $"Sản phẩm \"{p.Name}\" chỉ còn {p.StockQuantity} trong kho.");
                    }
                }

                // All good — create order (so we have order.Id for logs)
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
                await db.SaveChangesAsync(); // order.Id được gán

                _logger.LogInformation("Created Order Id={OrderId}", order.Id);

                // Resolve friendly placedBy (email/username/customerName) if possible
                string placedBy = !string.IsNullOrWhiteSpace(customerName) ? customerName : "Khách lẻ";
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    try
                    {
                        // ApplicationDbContext is Identity-aware; try to read IdentityUser
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
                        // fallback
                        placedBy = !string.IsNullOrWhiteSpace(customerName) ? customerName : userId;
                    }
                }

                // Now create OrderDetails, decrement stock and add InventoryLogs
                foreach (var ci in items)
                {
                    // It's possible multiple lines refer to same product; use the tracked product
                    var product = products[ci.ProductId];

                    // Double check availability (defensive)
                    if (product.StockQuantity < ci.Quantity)
                    {
                        // unexpected: should have been caught earlier
                        _logger.LogWarning("Unexpected stock shortage for ProductId={Pid} during commit. Rolling back.", product.Id);
                        await tx.RollbackAsync();
                        return new OrderResult(false, null, $"Sản phẩm \"{product.Name}\" không đủ tồn khi xử lý đơn.");
                    }

                    int oldQty = product.StockQuantity;
                    product.StockQuantity -= ci.Quantity;
                    db.Products.Update(product);

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        ProductName = ci.ProductName,
                        Quantity = ci.Quantity,
                        Price = ci.Price
                    };
                    db.OrderDetails.Add(orderDetail);

                    var log = new InventoryLog
                    {
                        ProductId = product.Id,
                        OldQuantity = oldQty,
                        QuantityChange = -ci.Quantity,
                        NewQuantity = product.StockQuantity,
                        Reason = $"Bán hàng - Đơn #{order.Id} bởi {placedBy}",
                        Timestamp = DateTime.UtcNow
                    };
                    db.InventoryLogs.Add(log);

                    _logger.LogInformation("Product {Pid}: {Old} -> {New} (change {Change})", product.Id, oldQty, product.StockQuantity, -ci.Quantity);
                }

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

        /// <summary>
        /// Wrapper gọi CreateOrderAsync từ Checkout (giữ interface cũ).
        /// </summary>
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
