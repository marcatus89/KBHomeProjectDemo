using System.Linq;
using System.Threading.Tasks;
using DoAnTotNghiep.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ProductsController(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20)
        {
            if (pageSize > 100) pageSize = 100;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var q = db.Products.AsNoTracking().Where(p => p.IsVisible);

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(p => p.Id)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(p => new
                               {
                                   p.Id, p.Name, p.Price, p.ImageUrl, p.StockQuantity, p.CategoryId
                               })
                               .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            return Ok(p);
        }
    }
}
