using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public InventoryController(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Lấy danh sách InventoryLog cho productId, optional from/to (UTC)
        /// GET /api/inventory/logs/5?from=2025-01-01&to=2025-12-31
        /// </summary>
        [HttpGet("logs/{productId:int}")]
        public async Task<IActionResult> GetLogs(int productId, DateTime? from = null, DateTime? to = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            IQueryable<InventoryLog> q = db.InventoryLogs.AsNoTracking()
                                                          .Where(l => l.ProductId == productId);

            if (from.HasValue)
            {
                q = q.Where(l => l.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                q = q.Where(l => l.Timestamp <= to.Value);
            }

            var list = await q.OrderByDescending(l => l.Timestamp)
                              .ThenByDescending(l => l.Id)
                              .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Lấy log theo id
        /// </summary>
        [HttpGet("log/{id:int}")]
        public async Task<IActionResult> GetLogById(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var log = await db.InventoryLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (log == null) return NotFound();
            return Ok(log);
        }

        /// <summary>
        /// Tạo 1 InventoryLog (dùng cho thử nghiệm hoặc API quản trị).
        /// </summary>
        [HttpPost("log")]
        public async Task<IActionResult> CreateLog([FromBody] InventoryLog model)
        {
            if (model == null) return BadRequest("Model null");

            // ensure timestamp present if not set
            if (model.Timestamp == default) model.Timestamp = DateTime.UtcNow;

            await using var db = await _dbFactory.CreateDbContextAsync();
            db.InventoryLogs.Add(model);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLogById), new { id = model.Id }, model);
        }
    }
}
