// Services/PurchaseOrderService.cs
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
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
            // Use DB identity to obtain unique Id safely
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            _dbContext.PurchaseOrders.Add(purchaseOrder);
            await _dbContext.SaveChangesAsync(); // Id assigned by DB

            purchaseOrder.PurchaseOrderNumber = $"PN-{purchaseOrder.Id:D5}";
            await _dbContext.SaveChangesAsync();

            await tx.CommitAsync();
        }
    }
}
