using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

/// <summary>
/// Contract for wallet operations including currency changes.
///
/// Design notes:
/// - Every method is user-scoped: pass the caller's userId so the implementation cannot return
///   or mutate another user's wallet.
/// - Currency changes are the interesting part: they are split into Preview (read-only, for the UI
///   to show "1 EUR ≈ 1.08 USD → 1,234 USD") and Change (writes the new currency + converts
///   the balance and every transaction amount on the wallet).
/// - DeleteWalletAsync throws while the wallet still has transactions, because the DB cascade
///   would otherwise silently delete them.
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// All wallets of the user, ordered so Spending wallets come first, then alphabetically.
    /// </summary>
    Task<List<Wallet>> GetAllByUserIdAsync(long userId);

    /// <summary>
    /// One wallet, only if it belongs to the user. Null otherwise.
    /// </summary>
    Task<Wallet?> GetByIdAsync(long walletId, long userId);

    /// <summary>
    /// Creates a new wallet for the user. Initial balance cannot be negative.
    /// </summary>
    Task<Wallet> CreateWalletAsync(string name, string type, decimal initialBalance, string currency, long userId);

    /// <summary>
    /// Changes an existing wallet of the user; returns null if the wallet is not theirs / not found.
    /// </summary>
    Task<Wallet?> UpdateWalletAsync(long walletId, string name, string type, decimal balance, string currency, long userId);

    /// <summary>
    /// Deletes a wallet of the user. Returns false if it is not theirs / not found.
    /// Throws InvalidOperationException while the wallet still has transactions.
    /// </summary>
    Task<bool> DeleteWalletAsync(long walletId, long userId);

    /// <summary>
    /// Returns the user's first wallet (ordered by Id) — the one created at registration.
    /// The profile page uses this as the "main wallet" when changing currency.
    /// </summary>
    Task<Wallet?> GetMainWalletAsync(long userId);

    /// <summary>
    /// Non-destructive preview of a currency change (fetch the rate, compute the converted
    /// balance, but do NOT save).
    /// </summary>
    Task<CurrencyPreview> PreviewCurrencyChangeAsync(long userId, long walletId, string targetCurrency);

    /// <summary>
    /// Actually changes the wallet's currency. If the wallet has money or transactions,
    /// both the balance and every transaction amount are converted at the current rate.
    /// </summary>
    Task<CurrencyChangeResult> ChangeCurrencyAsync(long userId, long walletId, string newCurrency);
}
