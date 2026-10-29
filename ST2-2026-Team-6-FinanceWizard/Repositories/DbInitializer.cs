using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Repositories;

/// <summary>
/// Seeds reference data on startup (categories, merchants, tags) and a demo user + wallets.
///
/// Design notes:
/// - Static class: no state, no DI, called once from Program.cs.
/// - MigrateAsync runs any pending EF migrations, so the DB schema is always up-to-date.
/// - Each "if not already any" guard means the seed is idempotent — running the app twice
///   does not create duplicates.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Ensure schema migrations are applied on startup
        await context.Database.MigrateAsync();

        // 2. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            await context.Categories.AddRangeAsync(
                new Category { Name = "Shopping", Icon = "cart" },
                new Category { Name = "Bills & Services", Icon = "bolt" },
                new Category { Name = "Entertainment", Icon = "film" }
            );
        }

        // 3. Seed Merchants
        if (!await context.Merchants.AnyAsync())
        {
            await context.Merchants.AddRangeAsync(
                new Merchant { Name = "Lidl" },
                new Merchant { Name = "Kaufland" },
                new Merchant { Name = "EVN" }
            );
        }

        // 4. Seed Tags
        if (!await context.Tags.AnyAsync())
        {
            await context.Tags.AddRangeAsync(
                new Tag { Name = "Essential" },
                new Tag { Name = "Monthly" },
                new Tag { Name = "Leisure" }
            );
        }

        // Save lookup entities
        await context.SaveChangesAsync();

        // 5. Seed Default User (Required to satisfy FK_Wallets_Users_UserId)
        if (!await context.Users.AnyAsync())
        {
            var defaultUser = new User
            {
                Username = "demo_user",
                Email = "demo@financewizard.local",
                PasswordHash = "AQAAAAIAAYagAAAAEJz9X..." // Default identity hash placeholder
            };

            await context.Users.AddAsync(defaultUser);
            await context.SaveChangesAsync(); // Save to generate defaultUser.Id

            // 6. Seed Wallets assigned to the Default User
            if (!await context.Wallets.AnyAsync())
            {
                await context.Wallets.AddRangeAsync(
                    new Wallet { Name = "Main Account", Type = WalletTypes.Spending, Balance = 2500.00m, Currency = "EUR", UserId = defaultUser.Id },
                    new Wallet { Name = "Savings", Type = WalletTypes.Savings, Balance = 1200.00m, Currency = "EUR", UserId = defaultUser.Id }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
