using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Services
{
    public class PurchaseOrderService
    {
        private readonly ApplicationDbContext _dbContext;

        public PurchaseOrderService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder)
        {
            var lastOrder = await _dbContext.PurchaseOrders
                                .OrderByDescending(po => po.Id)
                                .FirstOrDefaultAsync();
            
            int nextId = (lastOrder?.Id ?? 0) + 1;
            purchaseOrder.PurchaseOrderNumber = $"PN-{nextId:D5}";

            _dbContext.PurchaseOrders.Add(purchaseOrder);
            await _dbContext.SaveChangesAsync();
        }
    }
}

