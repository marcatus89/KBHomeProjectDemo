using System.Linq;
using System.Threading.Tasks;
using DoAnTotNghiep.Data;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Services
{
    // Model chứa các số liệu thống kê của Dashboard
    public class DashboardStats
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
    }

    public class DashboardService
    {
        private readonly ApplicationDbContext _dbContext;

        public DashboardService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();

            // Lấy tất cả đơn hàng trừ những đơn đã hủy
            var validOrders = _dbContext.Orders.Where(o => o.Status != "Đã hủy");

            var totalRevenueDouble = await validOrders.SumAsync(o => (double)o.TotalAmount);
            stats.TotalRevenue = (decimal)totalRevenueDouble;
            stats.TotalOrders = await validOrders.CountAsync();
            stats.PendingOrders = await validOrders.CountAsync(o => o.Status == "Chờ xác nhận");

            return stats;
        }
    }
}
