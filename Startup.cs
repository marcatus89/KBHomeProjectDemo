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

            // Use DbContextFactory for Blazor Server to avoid DbContext concurrency issues
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Provide ApplicationDbContext as scoped (created from factory) so Identity/controllers can inject it
            services.AddScoped(provider =>
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

            // Identity (cookie-based for UI)
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddDefaultTokenProviders()
            .AddDefaultUI()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // Configure application cookie (Identity cookie settings)
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            // JWT configuration (for API access) — do NOT override default cookie scheme here
            var jwtSettings = Configuration.GetSection("Jwt");
            var jwtKey = jwtSettings["Key"] ?? "DefaultSuperSecretKey_ReplaceThis";
            var key = Encoding.UTF8.GetBytes(jwtKey);

            // Add authentication and add JwtBearer (without setting default scheme, so Identity cookie remains default for UI)
            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
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

            // Add response compression (improves Blazor Server perf)
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
            });

            // HttpClient factory (ServerAPI named)
            services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri(Configuration["ServerApiBaseAddress"] ?? "https://localhost:5001/");
            });
            services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

            // Add HttpContextAccessor if some services need it
            services.AddHttpContextAccessor();

            // MVC / Razor / Blazor
            services.AddControllers();
            services.AddRazorPages();
            services.AddServerSideBlazor()
                    .AddCircuitOptions(options => options.DetailedErrors = _env.IsDevelopment());

            // Application services (register lifetimes)
            services.AddScoped<ToastService>();
            services.AddScoped<CartService>();               // CartService must be Scoped for Blazor Server
            services.AddScoped<OrderService>();
            services.AddScoped<DashboardService>();
            services.AddScoped<PurchaseOrderService>();
            services.AddScoped<ReturnReceiptService>();


            // AuthenticationStateProvider custom (if you implemented one)
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

            // Protected browser storage (local/session) for Blazor Server
            services.AddScoped<ProtectedLocalStorage>();

            // Authorization
            services.AddAuthorization();

            // CORS policy (dev-friendly). Adjust allowed origins for production.
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowAnyOrigin(); // if you need cookies/credentials, use WithOrigins(...) and AllowCredentials()
                });
            });

            // Swagger/OpenAPI (useful for testing API)
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KBHome API", Version = "v1" });

                // Define the BearerAuth scheme that's in use
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter 'Bearer {token}' (without quotes). You can get token from /api/token/login",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                };
                c.AddSecurityDefinition("Bearer", securityScheme);

                var securityRequirement = new OpenApiSecurityRequirement
                {
                    { securityScheme, Array.Empty<string>() }
                };
                c.AddSecurityRequirement(securityRequirement);
            });

            // If you want to allow large file uploads for API, configure here (optional)
            // services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 104857600; });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Enable response compression before static files
            app.UseResponseCompression();

            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Enable swagger UI in development
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
            app.UseStaticFiles();
            app.UseRouting();

            // CORS - must go before Authentication/Authorization if APIs are called from browsers
            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
