// Controllers/OrdersController.cs
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
using DoAnTotNghiep.Models.Dto;

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
        /// Tạo đơn từ payload
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto req)
        {
            if (req == null) return BadRequest("Request null");
            if (req.Items == null || !req.Items.Any()) return BadRequest("Cart empty");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // determine user
            string? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Map DTO items -> domain CartItem
            var items = req.Items.Select(i => new CartItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName ?? string.Empty,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToArray();

            var total = items.Sum(i => i.Price * i.Quantity);

            var result = await _orderService.CreateOrderAsync(
                userId,
                string.IsNullOrWhiteSpace(req.CustomerName) ? (User?.Identity?.Name ?? "Khách lẻ") : req.CustomerName,
                req.PhoneNumber ?? "",
                req.ShippingAddress ?? "",
                items,
                total
            );

            if (!result.Success)
            {
                _logger.LogWarning("Create order failed: {Error}", result.ErrorMessage);
                return BadRequest(new { success = false, error = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, new { success = true, orderId = result.OrderId });
        }
    }
}
