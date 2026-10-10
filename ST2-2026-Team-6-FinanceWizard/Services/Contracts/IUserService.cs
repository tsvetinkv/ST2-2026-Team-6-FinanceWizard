using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface IUserService
    {
        Task<User> RegisterAsync(string username, string email, string password);

        Task<string> LoginAsync(string email, string password);

        Task<User?> GetByIdAsync(long userId);

        Task<User?> ValidateUserAsync(string email, string password);

    }
}
