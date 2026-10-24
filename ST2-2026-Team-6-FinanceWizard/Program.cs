using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Services;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register repositories and services
            builder.Services.AddScoped(typeof(IEfRepository<>), typeof(EfRepository<>));
            builder.Services.AddSingleton(builder.Configuration);
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IStatsService, StatsService>();
            builder.Services.AddScoped<IDashboardFacade, DashboardFacade>();
            builder.Services.AddScoped<IAiCategoryService, AiCategoryService>();

            // Settings for Cookie Auth
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}