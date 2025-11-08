using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DoAnTotNghiep.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace DoAnTotNghiep
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Tạo một scope để lấy các dịch vụ và thực hiện migration/seed
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    var env = services.GetRequiredService<IHostEnvironment>();
                    var config = services.GetRequiredService<IConfiguration>();

                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // Áp migration (với retry nhẹ nếu muốn)
                    var retry = 0;
                    var maxRetry = 5;
                    while (true)
                    {
                        try
                        {
                            await context.Database.MigrateAsync();
                            logger.LogInformation("Database migrations applied successfully.");
                            break;
                        }
                        catch (Exception ex) when (retry < maxRetry)
                        {
                            retry++;
                            logger.LogWarning(ex, "Migration failed (attempt {Attempt}/{Max}), retrying in 3s...", retry, maxRetry);
                            await Task.Delay(TimeSpan.FromSeconds(3));
                        }
                    }

                    // Seed dữ liệu mẫu (synchronous/async tuỳ implement)
                    SeedData.Initialize(context, logger);
                    await SeedIdentity.SeedRolesAndAdminAsync(services);

                    // --- DEV ONLY: tạo token cho admin và in ra console ---
                    if (env.IsDevelopment())
                    {
                        try
                        {
                            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                            var adminEmail = "admin@kbhome.vn"; // sửa nếu email admin khác
                            var admin = await userManager.FindByEmailAsync(adminEmail);

                            if (admin != null)
                            {
                                var jwtSection = config.GetSection("Jwt");
                                var keyString = jwtSection["Key"];
                                var issuer = jwtSection["Issuer"];
                                var audience = jwtSection["Audience"];
                                if (string.IsNullOrWhiteSpace(keyString) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
                                {
                                    logger.LogWarning("Jwt configuration (Key/Issuer/Audience) is missing — cannot generate dev token.");
                                }
                                else
                                {
                                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
                                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                                    var claims = new List<Claim>
                                    {
                                        new Claim(JwtRegisteredClaimNames.Sub, admin.Id),
                                        new Claim(JwtRegisteredClaimNames.Email, admin.Email ?? string.Empty),
                                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                                    };

                                    var roles = await userManager.GetRolesAsync(admin);
                                    foreach (var r in roles)
                                    {
                                        claims.Add(new Claim(ClaimTypes.Role, r));
                                    }

                                    double expiryMinutes = 60;
                                    if (double.TryParse(jwtSection["ExpiryMinutes"], out var parsed)) expiryMinutes = parsed;

                                    var token = new JwtSecurityToken(
                                        issuer: issuer,
                                        audience: audience,
                                        claims: claims,
                                        expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                                        signingCredentials: creds
                                    );

                                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                                    Console.WriteLine();
                                    Console.WriteLine("-----------------------------------------------------------");
                                    Console.WriteLine("DEVELOPMENT JWT for admin ({0}):", adminEmail);
                                    Console.WriteLine(tokenString);
                                    Console.WriteLine("Expires (UTC): {0:u}", token.ValidTo);
                                    Console.WriteLine("-----------------------------------------------------------");
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                logger.LogWarning("Admin user with email '{Email}' not found; cannot create dev JWT.", adminEmail);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to create development JWT token.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Reuse existing logger variable instead of redeclaring
                    logger.LogError(ex, "An error occurred during database initialization or seeding.");
                    throw;
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
