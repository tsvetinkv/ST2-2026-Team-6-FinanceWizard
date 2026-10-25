#nullable enable
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Services;
using ST2_2026_Team_6_FinanceWizard.Services.AI;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // AI Integration: Distributed memory cache and session state for Advisor Chat history
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // 1. Initialize Singleton Database Connection
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

            DatabaseConnection.Instance.Initialize(connectionString);

            // 2. Register ApplicationDbContext as Scoped using Connection String from Singleton
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(DatabaseConnection.Instance.ConnectionString));

            // Register repositories and services
            builder.Services.AddScoped(typeof(EfRepository<>));
            // Decorator: всяко IEfRepository<T> се получава обвито в LoggingRepository<T>
            builder.Services.AddScoped(typeof(IEfRepository<>), typeof(LoggingRepository<>));
            builder.Services.AddSingleton(builder.Configuration);
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IStatsService, StatsService>();
            builder.Services.AddScoped<IDashboardFacade, DashboardFacade>();
            builder.Services.AddScoped<IAiCategoryService, AiCategoryService>();

            builder.Services.AddScoped<ILogoStorage, LogoStorage>();

            // Downloads images for merchant logos (time-limited, redirects not followed)
            builder.Services.AddHttpClient(LogoStorage.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceWizard/1.0");
            })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    ConnectTimeout = TimeSpan.FromSeconds(5)
                });

            // AI Integration: Configure AI Client Singleton & Strategy Pattern
            // Key comes from user-secrets (Gemini:ApiKey); model and timeout come from appsettings.json.
            var apiKey = builder.Configuration["Gemini:ApiKey"]
                ?? builder.Configuration["ExternalApis:GeminiApiKey"]
                ?? string.Empty;

            var geminiModel = builder.Configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";

            AiClientProvider.Instance.Initialize(apiKey, geminiModel, 30);

            builder.Services.AddHttpClient<GeminiProvider>(client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = AiClientProvider.Instance.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceWizard/1.0");
            });

            builder.Services.AddScoped<MockProvider>();

            // Dynamic Strategy Selection: Uses live Gemini provider if API Key is configured, otherwise falls back to MockProvider
            builder.Services.AddScoped<IAiProvider>(sp =>
            {
                return string.IsNullOrWhiteSpace(AiClientProvider.Instance.ApiKey)
                    ? sp.GetRequiredService<MockProvider>()
                    : sp.GetRequiredService<GeminiProvider>();
            });

            // Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

            var app = builder.Build();

            // Automatic Database Seeding on Startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await DbInitializer.SeedAsync(dbContext);
            }

            // Configure pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // AI Integration: Enable Session Middleware for chat history tracking
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}