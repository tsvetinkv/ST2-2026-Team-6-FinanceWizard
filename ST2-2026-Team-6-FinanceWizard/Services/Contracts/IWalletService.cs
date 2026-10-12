using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

public interface IWalletService
{
    // All wallets of the user (spending / savings), ordered by name
    Task<List<Wallet>> GetAllByUserIdAsync(long userId);

    // One wallet, only if it belongs to the user
    Task<Wallet?> GetByIdAsync(long walletId, long userId);

    // Creates a new wallet for the user
    Task<Wallet> CreateWalletAsync(string name, string type, decimal initialBalance, string currency, long userId);

    // Changes an existing wallet of the user; returns null if the wallet is not theirs / not found
    Task<Wallet?> UpdateWalletAsync(long walletId, string name, string type, decimal balance, string currency, long userId);

    // Deletes a wallet of the user. Returns false if it is not theirs / not found.
    // Throws InvalidOperationException while the wallet still has transactions.
    Task<bool> DeleteWalletAsync(long walletId, long userId);
}
