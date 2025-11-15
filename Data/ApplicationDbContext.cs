using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Identity;

namespace DoAnTotNghiep.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }
        public DbSet<InventoryAdjustmentRequest> InventoryAdjustmentRequests { get; set; }

        // ReturnReceipt entities
        public DbSet<ReturnReceipt> ReturnReceipts { get; set; }
        public DbSet<ReturnReceiptItem> ReturnReceiptItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Gọi base để Identity cấu hình mặc định
            base.OnModelCreating(builder);

            // ---------- Decimal precision ----------
            // Đặt precision cho các cột decimal để tránh cảnh báo & mất dữ liệu
            builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            builder.Entity<OrderDetail>().Property(od => od.Price).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<PurchaseOrderItem>().Property(pi => pi.UnitPrice).HasPrecision(18, 2);
            // Thêm cấu hình cho ReturnReceiptItem.UnitPrice
            builder.Entity<ReturnReceiptItem>().Property(rri => rri.UnitPrice).HasPrecision(18, 2);

            // ---------- Identity column sizing ----------
            // Các cấu hình này đảm bảo EF dùng nvarchar(450) cho các Id/khóa
            builder.Entity<IdentityUser>(b =>
            {
                b.Property(u => u.Id).HasMaxLength(450);
                b.Property(u => u.UserName).HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                b.Property(u => u.Email).HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasMaxLength(256);
            });

            builder.Entity<IdentityRole>(b =>
            {
                b.Property(r => r.Id).HasMaxLength(450);
                b.Property(r => r.Name).HasMaxLength(256);
                b.Property(r => r.NormalizedName).HasMaxLength(256);
            });

            builder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.Property(l => l.LoginProvider).HasMaxLength(128);
                b.Property(l => l.ProviderKey).HasMaxLength(128);
            });

            builder.Entity<IdentityUserToken<string>>(b =>
            {
                b.Property(t => t.LoginProvider).HasMaxLength(128);
                b.Property(t => t.Name).HasMaxLength(128);
            });

            builder.Entity<IdentityUserRole<string>>(b =>
            {
                b.Property(ur => ur.UserId).HasMaxLength(450);
                b.Property(ur => ur.RoleId).HasMaxLength(450);
            });

            builder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.Property(rc => rc.RoleId).HasMaxLength(450);
            });

            builder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.Property(uc => uc.UserId).HasMaxLength(450);
            });

            // Nếu cần thêm cấu hình khác cho các bảng mới, thêm ở đây.
        }
    }
}
