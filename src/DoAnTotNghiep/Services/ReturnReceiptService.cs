// Services/ReturnReceiptService.cs
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Services
{
    public class ReturnReceiptService
    {
        private readonly ApplicationDbContext _dbContext;

        public ReturnReceiptService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Tạo phiếu thu hồi (chỉ lưu và sinh mã). Không cập nhật tồn ở bước này.
        public async Task CreateReturnReceiptAsync(ReturnReceipt rr)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            await _dbContext.ReturnReceipts.AddAsync(rr);
            await _dbContext.SaveChangesAsync();

            rr.ReturnReceiptNumber = $"PH-{rr.Id:D5}";
            _dbContext.ReturnReceipts.Update(rr);
            await _dbContext.SaveChangesAsync();

            await tx.CommitAsync();
        }

        // Nhận phiếu thu hồi: cộng ReceivedQuantity vào tồn và tạo InventoryLog
        // receiver: tên user/ai nhập
        public async Task ReceiveReturnReceiptAsync(int returnReceiptId, string receiver)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            var rr = await _dbContext.ReturnReceipts
                            .Include(r => r.Items)
                                .ThenInclude(i => i.Product)
                            .FirstOrDefaultAsync(r => r.Id == returnReceiptId);

            if (rr == null) throw new Exception("Không tìm thấy phiếu thu hồi.");

            if (rr.Status != "Đã nhận hàng")
            {
                foreach (var it in rr.Items)
                {
                    var prod = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);
                    if (prod == null) continue;

                    int oldQty = prod.StockQuantity;
                    prod.StockQuantity += it.ReceivedQuantity;
                    _dbContext.Products.Update(prod);

                    var log = new InventoryLog
                    {
                        ProductId = prod.Id,
                        OldQuantity = oldQty,
                        QuantityChange = it.ReceivedQuantity,
                        NewQuantity = prod.StockQuantity,
                        Reason = $"Thu hồi do hủy đơn #{rr.RelatedOrderId} (nhập bởi {receiver})",
                        Timestamp = DateTime.UtcNow
                    };
                    await _dbContext.InventoryLogs.AddAsync(log);
                }

                rr.Status = "Đã nhận hàng";
                _dbContext.ReturnReceipts.Update(rr);
                await _dbContext.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
    }
}
