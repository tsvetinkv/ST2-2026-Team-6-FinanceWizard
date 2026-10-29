using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    /// <summary>
    /// Contract for user account management.
    ///
    /// Design notes:
    /// - Passwords are never passed around in plaintext beyond what's needed: register / login /
    ///   change-password / delete-account receive the plaintext from the form, hash it with BCrypt,
    ///   and never store or return it.
    /// - Errors (email in use, wrong password) are expressed as return values (null / false) so the
    ///   controller can render them as ModelState errors without a try/catch. Registration is the
    ///   exception (throws) because "email already exists" is a genuine business error.
    /// - The record types at the bottom (CurrencyChangeResult / CurrencyPreview) are the standard
    ///   way to return "success or error + data" without exceptions. The static factories
    ///   (Fail / Ok) make call sites readable and keep the success path and the error path
    ///   visually distinct.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user and creates their first (EUR, balance 0) wallet.
        /// Throws when the email is already registered.
        /// </summary>
        Task<User> RegisterAsync(string username, string email, string password);

        /// <summary>
        /// Legacy JWT login. Returns a JWT string on success, throws on failure.
        /// The app currently uses cookie auth; this method is kept for potential API use.
        /// </summary>
        Task<string> LoginAsync(string email, string password);

        Task<User?> GetByIdAsync(long userId);

        /// <summary>
        /// Returns the user when email + password are correct, otherwise null.
        /// Used by AccountController.Login.
        /// </summary>
        Task<User?> ValidateUserAsync(string email, string password);

        // Връща обновения потребител или null, ако не съществува
        Task<User?> UpdateUsernameAsync(long userId, string newUsername);

        // false = грешна текуща парола
        Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword);

        // false = грешна парола. При успех изтрива потребителя (каскадно: портфейл и транзакции)
        Task<bool> DeleteAccountAsync(long userId, string password);
    }

    /// <summary>
    /// Result of ChangeCurrencyAsync: success + the from/to codes + the rate used + how many wallets
    /// were updated + whether an actual conversion happened (Converted = false when the wallet was empty).
    ///
    /// Design notes:
    /// - A record: immutable and value-comparable, easy to assert in tests.
    /// - Fail / Ok are the only intended constructors at the call site, so the "Success" flag
    ///   cannot be accidentally set to false with non-null Error, etc.
    /// - The `with { Converted = false }` syntax (used in WalletService) lets a caller tweak one
    ///   property of an Ok result without reconstructing everything.
    /// </summary>
    public record CurrencyChangeResult(bool Success, string? Error = null, string? FromCode = null,
                                   string? ToCode = null, decimal Rate = 1m, int WalletCount = 0,
                                   bool Converted = true)
    {
        public static CurrencyChangeResult Fail(string error) => new(false, error);
        public static CurrencyChangeResult Ok(string? from, string to, decimal rate, int walletCount)
            => new(true, null, from, to, rate, walletCount);
    }

    /// <summary>
    /// Result of PreviewCurrencyChangeAsync: nothing is saved, this only reports what WOULD happen.
    /// Rate, Before and After are nullable because an empty wallet needs no rate and has no amounts.
    /// </summary>
    public record CurrencyPreview(bool Success, string? Error = null, string? ToCode = null, string? FromCode = null,
                                  decimal? Rate = null, decimal? Before = null, decimal After = 0m, int WalletCount = 0)
    {
        public static CurrencyPreview Fail(string error) => new(false, error);
        public static CurrencyPreview Ok(string to, string? from, decimal? rate, decimal? before, decimal after, int walletCount)
            => new(true, null, to, from, rate, before, after, walletCount);
    }
}
