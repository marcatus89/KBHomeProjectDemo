using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Hosting;

namespace DoAnTotNghiep.Data
{
    public static class SeedIdentity
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            var env = services.GetService<IHostEnvironment>();

            // --- TẠO VAI TRÒ ---
            string[] roleNames = { "Admin", "Sales", "Warehouse", "Logistics", "Purchasing" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{roleName}' created successfully.");
                    }
                    else
                    {
                        logger.LogError("Failed to create role {Role}: {Errors}", roleName,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // --- TẠO VÀ GÁN QUYỀN CHO ADMIN ---
            var adminEmail = "admin@kbhome.vn";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                // Lấy password từ ENV cho production; cho phép tạo password tạm ở Development
                var adminPassword = Environment.GetEnvironmentVariable("KBHOME_ADMIN_PASSWORD");
                bool usedGeneratedPassword = false;

                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    if (env != null && env.IsDevelopment())
                    {
                        // Development: tạo password tạm và bắt đổi
                        adminPassword = GenerateSecureDevPassword();
                        usedGeneratedPassword = true;
                        logger.LogWarning("KBHOME_ADMIN_PASSWORD not set. Creating development admin with a temporary password. Require password change on first login.");
                    }
                    else
                    {
                        logger.LogError("KBHOME_ADMIN_PASSWORD is not set. Aborting admin seeding for security.");
                        throw new InvalidOperationException("KBHOME_ADMIN_PASSWORD must be set as environment variable for admin creation.");
                    }
                }

                adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                else
                {
                    logger.LogInformation("Admin user created successfully.");

                    // Gán tất cả role cho admin
                    var addRolesRes = await userManager.AddToRolesAsync(adminUser, roleNames);
                    if (!addRolesRes.Succeeded)
                    {
                        logger.LogError("Failed to assign roles to admin: {Errors}", string.Join(", ", addRolesRes.Errors.Select(e => e.Description)));
                    }

                    // Nếu password do dev tạo, thêm claim bắt đổi mật khẩu
                    if (usedGeneratedPassword)
                    {
                        await userManager.AddClaimAsync(adminUser, new Claim("mustChangePassword", "true"));
                        logger.LogInformation("Added mustChangePassword claim for dev admin account.");
                        logger.LogInformation("Temporary admin credentials: {Email} / [temporary password set in development]. Please change immediately.", adminEmail);
                    }
                }
            }
            else
            {
                logger.LogInformation("Admin user already exists. Ensuring all roles are assigned.");
                await userManager.AddToRolesAsync(adminUser, roleNames);
            }

            // --- TẠO TÀI KHOẢN DEMO CHO CÁC PHÒNG BAN (CHỈ DEV IF ALLOWED) ---
            try
            {
                var allowDemo = Environment.GetEnvironmentVariable("KBHOME_CREATE_DEMO_USERS");
                if (env != null && env.IsDevelopment() && string.Equals(allowDemo, "true", StringComparison.OrdinalIgnoreCase))
                {
                    await CreateUserIfNotExists(userManager, logger, "purchasing@kbhome.vn", "Purchasing");
                    await CreateUserIfNotExists(userManager, logger, "warehouse@kbhome.vn", "Warehouse");
                    await CreateUserIfNotExists(userManager, logger, "sales@kbhome.vn", "Sales");
                    await CreateUserIfNotExists(userManager, logger, "logistics@kbhome.vn", "Logistics");
                }
                else
                {
                    logger.LogInformation("Demo users creation skipped (KBHOME_CREATE_DEMO_USERS != true or not Development).");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while creating demo users (non-fatal).");
            }
        }

        private static async Task CreateUserIfNotExists(UserManager<IdentityUser> userManager, ILogger logger, string email, string role)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                // Try to take demo password from env, else generate a one-time password for dev
                var demoPassword = Environment.GetEnvironmentVariable("KBHOME_DEMO_PASSWORD");
                var usedGeneratedPassword = false;
                if (string.IsNullOrWhiteSpace(demoPassword))
                {
                    demoPassword = GenerateSecureDevPassword();
                    usedGeneratedPassword = true;
                }

                var newUser = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(newUser, demoPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, role);
                    logger.LogInformation("User '{Email}' created with role '{Role}'.", email, role);
                    if (usedGeneratedPassword)
                    {
                        logger.LogInformation("Demo user '{Email}' was created with a generated password. Please change it on first login.", email);
                    }
                }
                else
                {
                    logger.LogError("Failed to create demo user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Sinh mật khẩu dev tạm thời (phải thay đổi ngay). Độ dài & ký tự đảm bảo policy cơ bản.
        private static string GenerateSecureDevPassword()
        {
            // Not cryptographically ideal for prod — intended for dev only.
            var guid = Guid.NewGuid().ToString("N");
            return "Dev!" + guid.Substring(0, 8) + "aA1!";
        }
    }
}
