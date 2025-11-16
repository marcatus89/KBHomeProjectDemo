using System.Threading.Tasks;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentsController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public ShipmentsController(IDbContextFactory<ApplicationDbContext> dbFactory) => _dbFactory = dbFactory;

        [HttpPost("create")]
        [Authorize(Roles = "Logistics,Admin")]
        public async Task<IActionResult> Create([FromBody] Shipment s)
        {
            if (s == null) return BadRequest();
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (await db.Shipments.AnyAsync(x => x.OrderId == s.OrderId))
                return BadRequest("Shipment for this order already exists");

            // set CreatedAt if property exists
            var prop = s.GetType().GetProperty("CreatedAt");
            if (prop != null && prop.PropertyType == typeof(System.DateTime))
                prop.SetValue(s, System.DateTime.UtcNow);

            db.Shipments.Add(s);

            var ord = await db.Orders.FirstOrDefaultAsync(o => o.Id == s.OrderId);
            if (ord != null)
            {
                ord.Status = "Đang giao";
                db.Orders.Update(ord);
            }

            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = s.Id }, s);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var sh = await db.Shipments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return sh == null ? NotFound() : Ok(sh);
        }
    }
}
