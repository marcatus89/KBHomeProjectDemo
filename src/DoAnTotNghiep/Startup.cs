using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace DoAnTotNghiep
{
    public class Startup
    {
        private readonly IConfiguration Configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Connection string
            var connectionString = Configuration.GetConnectionString("DefaultConnection");

            // Use DbContextFactory
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped(provider =>
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

            // Identity configuration
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddDefaultTokenProviders()
            .AddDefaultUI()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // Cookie settings
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;

                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = _env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });

            // JWT
            var jwtSettings = Configuration.GetSection("Jwt");
            var jwtKey = jwtSettings["Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                if (!_env.IsDevelopment())
                    throw new InvalidOperationException("JWT key missing");

                using (var sp = services.BuildServiceProvider())
                {
                    var devLogger = sp.GetRequiredService<ILogger<Startup>>();
                    devLogger.LogWarning("Using development fallback JWT key.");
                }

                jwtKey = "DevReplaceThisKey_DoNotUseInProd_ChangeIt";
            }

            var key = Encoding.UTF8.GetBytes(jwtKey);

            services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !_env.IsDevelopment();
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSettings["Issuer"]),
                    ValidateAudience = !string.IsNullOrWhiteSpace(jwtSettings["Audience"]),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Compression
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes
                    .Concat(new[] { "application/octet-stream" });
            });

            // HttpClient
            services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri(Configuration["ServerApiBaseAddress"] ?? "https://localhost:5001/");
            });
            services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

            services.AddHttpContextAccessor();

            services.AddControllers();
            services.AddRazorPages();
            services.AddServerSideBlazor()
                .AddCircuitOptions(options => options.DetailedErrors = _env.IsDevelopment());

            // Services
            services.AddScoped<ToastService>();
            services.AddScoped<CartService>();
            services.AddScoped<OrderService>();
            services.AddScoped<DashboardService>();
            services.AddScoped<PurchaseOrderService>();
            services.AddScoped<ReturnReceiptService>();
            services.AddScoped<TicketService>();
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            services.AddScoped<ProtectedLocalStorage>();

            services.AddAuthorization();

            // CORS
            var allowedOrigins = Configuration["AllowedOrigins"];
            if (string.IsNullOrWhiteSpace(allowedOrigins))
            {
                if (_env.IsDevelopment())
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("CorsPolicy", builder =>
                        {
                            builder.AllowAnyHeader()
                                   .AllowAnyMethod()
                                   .AllowAnyOrigin();
                        });
                    });
                }
                else throw new Exception("AllowedOrigins missing");
            }
            else
            {
                var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();

                services.AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy", builder =>
                    {
                        builder.WithOrigins(origins)
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    });
                });
            }

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "KBHome API",
                    Version = "v1"
                });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter: Bearer {token}",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                };

                c.AddSecurityDefinition("Bearer", securityScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, Array.Empty<string>() }
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KBHome API v1");
                    c.RoutePrefix = "swagger";
                });
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // ============================
            // ✅ FIX FULL CSP HERE
            // ============================
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "no-referrer-when-downgrade";
                context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";

                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'self'; " +

                    // JS
                    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com; " +

                    // CSS
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com; " +

                    // Fonts (fix missing icons)
                    "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +

                    // Images
                    "img-src 'self' data: blob: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +

                    // Blazor + Websocket
                    "connect-src 'self' https://localhost:* http://localhost:* ws://localhost:* wss://localhost:*; " +

                    "frame-ancestors 'none';";

                await next();
            });

            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            logger.LogInformation("Startup configured. Environment: {Env}", env.EnvironmentName);
        }
    }
}
