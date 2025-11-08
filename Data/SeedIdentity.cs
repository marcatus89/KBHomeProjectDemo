using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DoAnTotNghiep.Data
{
    public static class SeedIdentity
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            // --- TẠO VAI TRÒ ---
            // Danh sách tất cả các vai trò cần có trong hệ thống
            string[] roleNames = { "Admin", "Sales", "Warehouse", "Logistics", "Purchasing" };
            foreach (var roleName in roleNames)
            {
                // Chỉ tạo nếu vai trò chưa tồn tại
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{roleName}' created successfully.");
                    }
                }
            }

            // --- TẠO VÀ GÁN QUYỀN CHO ADMIN ---
            var adminEmail = "admin@kbhome.vn";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, "Password123!");
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully.");
                    // Gán TẤT CẢ các vai trò cho Admin
                    await userManager.AddToRolesAsync(adminUser, roleNames);
                    logger.LogInformation("Admin user assigned all roles.");
                }
            }
            else
            {
                // Đảm bảo Admin luôn có đủ tất cả các quyền, kể cả khi chạy lại seed
                logger.LogInformation("Admin user already exists. Ensuring all roles are assigned.");
                await userManager.AddToRolesAsync(adminUser, roleNames);
            }
            
            // --- TẠO TÀI KHOẢN DEMO CHO CÁC PHÒNG BAN ---
            await CreateUserIfNotExists(userManager, logger, "purchasing@kbhome.vn", "Password123!", "Purchasing");
            await CreateUserIfNotExists(userManager, logger, "warehouse@kbhome.vn", "Password123!", "Warehouse");
            await CreateUserIfNotExists(userManager, logger, "sales@kbhome.vn", "Password123!", "Sales");
            await CreateUserIfNotExists(userManager, logger, "logistics@kbhome.vn", "Password123!", "Logistics");
        }

        private static async Task CreateUserIfNotExists(UserManager<IdentityUser> userManager, ILogger logger, string email, string password, string role)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var newUser = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, role);
                    logger.LogInformation($"User '{email}' created with role '{role}'.");
                }
            }
        }
    }
}

