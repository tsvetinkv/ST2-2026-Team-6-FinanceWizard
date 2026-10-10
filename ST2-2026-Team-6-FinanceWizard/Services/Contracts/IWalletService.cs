using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface IWalletService
    {
        // Get the user's wallet details
        Task<Wallet> GetByUserIdAsync(long userId);

        // Create the initial wallet when user registers
        Task CreateWalletAsync(decimal initialBalance, string currency, long userId);

        // Check if user has enough money (Optional helper)
        Task<bool> HasSufficientFundsAsync(long userId, decimal amount);

        //update the wallet balance
        Task UpdateBalanceAsync(long userId, decimal newBalance);
    }
}
