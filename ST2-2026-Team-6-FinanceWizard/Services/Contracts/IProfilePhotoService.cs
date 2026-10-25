namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface IProfilePhotoService
    {
        string? GetUrl(long userId);
        /// <returns>An error message, or null when the photo was saved.</returns>
        Task<string?> SaveAsync(long userId, IFormFile file);
        void Delete(long userId);
    }
}