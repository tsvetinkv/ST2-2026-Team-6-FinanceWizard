using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface IUserService
    {
        Task<User> RegisterAsync(string username, string email, string password);

        Task<string> LoginAsync(string email, string password);

        Task<User?> GetByIdAsync(long userId);

        Task<User?> ValidateUserAsync(string email, string password);

        // Връща обновения потребител или null, ако не съществува
        Task<User?> UpdateUsernameAsync(long userId, string newUsername);

        // false = грешна текуща парола
        Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword);

        // false = грешна парола. При успех изтрива потребителя (каскадно: портфейл и транзакции)
        Task<bool> DeleteAccountAsync(long userId, string password);

    }
    public record CurrencyChangeResult(bool Success, string? Error = null, string? FromCode = null,
                                   string? ToCode = null, decimal Rate = 1m, int WalletCount = 0,
                                   bool Converted = true)
    {
        public static CurrencyChangeResult Fail(string error) => new(false, error);
        public static CurrencyChangeResult Ok(string? from, string to, decimal rate, int walletCount)
            => new(true, null, from, to, rate, walletCount);
    }

    public record CurrencyPreview(bool Success, string? Error = null, string? ToCode = null, string? FromCode = null,
                                  decimal? Rate = null, decimal? Before = null, decimal After = 0m, int WalletCount = 0)
    {
        public static CurrencyPreview Fail(string error) => new(false, error);
        public static CurrencyPreview Ok(string to, string? from, decimal? rate, decimal? before, decimal after, int walletCount)
            => new(true, null, to, from, rate, before, after, walletCount);
    }
}
