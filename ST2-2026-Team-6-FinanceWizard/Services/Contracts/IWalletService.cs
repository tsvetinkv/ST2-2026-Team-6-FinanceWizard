using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

public interface IWalletService
{
    // Gets the user's wallet, or null if none exists
    Task<Wallet?> GetByUserIdAsync(long userId);

    // Creates the initial wallet when a user registers
    Task CreateWalletAsync(decimal initialBalance, string currency, long userId);

    // Checks whether the user has enough funds
    Task<bool> HasSufficientFundsAsync(long userId, decimal amount);

    // Updates the wallet balance
    Task UpdateBalanceAsync(long userId, decimal newBalance);
}