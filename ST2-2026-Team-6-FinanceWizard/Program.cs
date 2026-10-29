#nullable enable
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Services;
using ST2_2026_Team_6_FinanceWizard.Services.AI;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard
{
    /// <summary>
    /// Application entry point. Builds the DI container, wires up middleware, and seeds the DB.
    ///
    /// Design patterns visible in this file:
    /// - SINGLETON (DatabaseConnection, AiClientProvider): one global instance for configuration
    ///   that is identical across requests.
    /// - SCOPED services (DbContext, repositories, services): one instance per HTTP request,
    ///   shared by all repositories in that request so they can share one unit of work.
    /// - DECORATOR (LoggingRepository<T> over EfRepository<T>): logged transparently.
    /// - STRATEGY (IAiProvider): Gemini or Mock, selected at startup based on configuration.
    /// - ABSTRACTION + DI everywhere: the app depends on interfaces, not concrete types,
    ///   so services can be swapped, tested, or decorated without touching consumers.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // MVC + views; also makes model binding / validation available in the views.
            builder.Services.AddControllersWithViews();

            // AI Integration: Distributed memory cache and session state for Advisor Chat history.
            // Session is required by AssistantController, which keeps chat history per browser session
            // (no DB table is needed for this). IdleTimeout is the "inactivity" window.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;         // the session cookie cannot be read by JS
                options.Cookie.IsEssential = true;      // survives "reject non-essential cookies"
            });

            // 1. Initialize Singleton Database Connection.
            // DatabaseConnection.Instance is the SINGLETON pattern: exactly one instance,
            // created lazily, holding the connection string for the whole app lifetime.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

            DatabaseConnection.Instance.Initialize(connectionString);

            // 2. Register ApplicationDbContext as Scoped using Connection String from Singleton.
            // Scoped = one instance per HTTP request. This is the "unit of work" shared by
            // all repositories, so a single SaveChanges commits changes from multiple repos together.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(DatabaseConnection.Instance.ConnectionString));

            // Register repositories and services.
            // EfRepository<T> is registered as itself so the LoggingRepository decorator
            // can wrap it. Then IEfRepository<T> is mapped to the decorator, so every consumer
            // gets a logged repository without knowing it.
            builder.Services.AddScoped(typeof(EfRepository<>));
            // Decorator: всяко IEfRepository<T> се получава обвито в LoggingRepository<T>
            builder.Services.AddScoped(typeof(IEfRepository<>), typeof(LoggingRepository<>));

            // Application services. All are user-scoped (they take userId in every method)
            // and share the same DbContext instance within a request.
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IStatsService, StatsService>();
            builder.Services.AddScoped<IDashboardFacade, DashboardFacade>();
            builder.Services.AddScoped<IAiCategoryService, AiCategoryService>();

            builder.Services.AddScoped<ILogoStorage, LogoStorage>();

            // Memory cache backs the exchange rate adapter (rates cached for 1 hour).
            builder.Services.AddMemoryCache();

            // Typed HttpClient for the exchange-rate adapter. BaseAddress and Timeout
            // are set once and apply to every call made by ExchangeRateService.
            builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>(client =>
            {
                client.BaseAddress = new Uri("https://api.frankfurter.dev/");
                client.Timeout = TimeSpan.FromSeconds(8);
            });

            builder.Services.AddScoped<IProfilePhotoService, ProfilePhotoService>();

            // Named HttpClient for downloading merchant logos.
            // SocketsHttpHandler settings:
            // - AllowAutoRedirect = false   → prevents redirect chains (SSRF safety)
            // - ConnectTimeout                → fails fast if the remote host is unreachable
            // - UserAgent                     → identifies the app to remote servers
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

            // AI Integration: Configure AI Client Singleton & Strategy Pattern.
            // The key comes from user-secrets (Gemini:ApiKey); model and timeout come from appsettings.json.
            // This is the SINGLETON pattern again: one config object for the whole app.
            var apiKey = builder.Configuration["Gemini:ApiKey"]
                ?? builder.Configuration["ExternalApis:GeminiApiKey"]
                ?? string.Empty;

            var geminiModel = builder.Configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";

            AiClientProvider.Instance.Initialize(apiKey, geminiModel, 30);

            // Typed HttpClient for the Gemini provider. The BaseAddress is the public API root;
            // the provider appends "v1beta/models/{model}:generateContent".
            builder.Services.AddHttpClient<GeminiProvider>(client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = AiClientProvider.Instance.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceWizard/1.0");
            });

            // The offline mock is registered as a concrete type so the factory below can resolve it.
            builder.Services.AddScoped<MockProvider>();

            // Dynamic Strategy Selection: uses the live Gemini provider when an API key is configured,
            // otherwise falls back to MockProvider.
            //
            // This is the STRATEGY pattern: the application only sees IAiProvider,
            // and the concrete implementation is chosen once, at composition time.
            // Switching to Gemini later (adding the key) requires no code changes, just a restart.
            builder.Services.AddScoped<IAiProvider>(sp =>
            {
                return string.IsNullOrWhiteSpace(AiClientProvider.Instance.ApiKey)
                    ? sp.GetRequiredService<MockProvider>()
                    : sp.GetRequiredService<GeminiProvider>();
            });

            // Cookie Authentication.
            // LoginPath / AccessDeniedPath tell the cookie middleware where to redirect.
            // ExpireTimeSpan is how long a session lasts without re-login.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

            var app = builder.Build();

            // Automatic Database Seeding on Startup.
            // A short-lived scope is created for the seeding work so it does not interfere with
            // request-scoped state. DbInitializer is idempotent (guards with "AnyAsync").
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await DbInitializer.SeedAsync(dbContext);
            }

            // Configure pipeline.
            // The ORDER matters:
            //   1. Exception handling and HSTS (production only)
            //   2. HTTPS redirection
            //   3. Static files (CSS / JS / images)
            //   4. Routing
            //   5. Authentication  → populates User from the cookie
            //   6. Authorization   → [Authorize] attributes can read User
            //   7. Session         → after auth, before MVC (AssistantController uses it)
            //   8. MVC endpoints
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

            // AI Integration: enable Session Middleware for chat history tracking.
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
