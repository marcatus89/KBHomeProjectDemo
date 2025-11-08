using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using DoAnTotNghiep.Services;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly OrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IDbContextFactory<ApplicationDbContext> dbFactory,
                                OrderService orderService,
                                ILogger<OrdersController> logger)
        {
            _dbFactory = dbFactory;
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy đơn theo id (includes details)
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .Include(o => o.Shipment)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return Ok(order);
        }

        /// <summary>
        /// Tạo đơn từ payload: dùng OrderService để đảm bảo transaction, logs, stock logic.
        /// Model: Order + Items (CartItem)
        /// </summary>
        public class CreateOrderRequest
        {
            public Order Order { get; set; } = new();
            public CartItem[] Items { get; set; } = Array.Empty<CartItem>();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            if (req == null) return BadRequest("Request null");
            if (req.Items == null || req.Items.Length == 0) return BadRequest("Cart empty");

            // determine user
            string? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var total = req.Items.Sum(i => i.Price * i.Quantity);

            var result = await _orderService.CreateOrderAsync(
                userId,
                string.IsNullOrWhiteSpace(req.Order.CustomerName) ? (User?.Identity?.Name ?? "Khách lẻ") : req.Order.CustomerName,
                req.Order.PhoneNumber ?? "",
                req.Order.ShippingAddress ?? "",
                req.Items,
                total
            );

            if (!result.Success)
            {
                _logger.LogWarning("Create order failed: {Error}", result.ErrorMessage);
                return BadRequest(new { success = false, error = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, new { success = true, orderId = result.OrderId });
        }

        // (tùy: thêm API để huỷ đơn, list orders, ...)
    }
}
