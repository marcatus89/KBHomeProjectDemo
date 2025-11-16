using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DoAnTotNghiep.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

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

                    // Kiểm tra connection string trước khi lấy context để tránh lỗi "ConnectionString property has not been initialized."
                    var connectionString = config.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        logger.LogWarning("ConnectionStrings:DefaultConnection is not configured. Skipping migrations and seeding. " +
                                          "Set ConnectionStrings:DefaultConnection via environment variable, user-secrets or appsettings to enable migrations.");
                    }
                    else
                    {
                        var context = services.GetRequiredService<ApplicationDbContext>();

                        // Áp migration với retry + exponential backoff
                        var maxRetry = 5;
                        var delaySeconds = 2;
                        for (int attempt = 1; attempt <= maxRetry; attempt++)
                        {
                            try
                            {
                                await context.Database.MigrateAsync();
                                logger.LogInformation("Database migrations applied successfully.");
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (attempt == maxRetry)
                                {
                                    logger.LogError(ex, "Database migration failed after {Attempts} attempts.", maxRetry);
                                    throw; // để outer catch xử lý / dừng app
                                }

                                logger.LogWarning(ex, "Migration failed (attempt {Attempt}/{Max}), retrying in {Delay}s...", attempt, maxRetry, delaySeconds);
                                // exponential backoff with cap
                                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                                delaySeconds = Math.Min(delaySeconds * 2, 30);
                            }
                        }

                        // Seed dữ liệu mẫu (synchronous/async tuỳ implement)
                        try
                        {
                            SeedData.Initialize(context, logger);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "SeedData.Initialize failed.");
                            // don't rethrow seed errors necessarily, but you can choose to fail-fast
                        }

                        await SeedIdentity.SeedRolesAndAdminAsync(services);
                    }

                    // --- DEV ONLY: nếu cần thông tin debug, dùng logger ở mức Debug — KHÔNG in token ra console ---
                }
                catch (Exception ex)
                {
                    // Reuse the existing logger declared above
                    logger.LogError(ex, "An error occurred during database initialization or seeding.");
                    throw;
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
