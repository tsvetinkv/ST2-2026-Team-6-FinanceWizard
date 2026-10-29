namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    /// <summary>
    /// Contract for storing one profile photo per user.
    ///
    /// Design pattern: ABSTRACTION over file-system concerns.
    /// - AccountController calls GetUrl / SaveAsync / Delete; it never touches wwwroot directly.
    /// - The concrete ProfilePhotoService handles: MIME sniffing, size limits, cache busting (?v=...),
    ///   and "replace" semantics (SaveAsync deletes the old file first).
    /// </summary>
    public interface IProfilePhotoService
    {
        /// <summary>
        /// Returns the URL of the user's photo, or null if they have not uploaded one.
        /// Implementations should append a cache-busting query so a replace is visible immediately.
        /// </summary>
        string? GetUrl(long userId);

        /// <summary>
        /// Saves a new photo, replacing any previous one.
        /// </summary>
        /// <returns>An error message, or null when the photo was saved.</returns>
        Task<string?> SaveAsync(long userId, IFormFile file);

        /// <summary>
        /// Removes the user's photo (no-op if there is none).
        /// </summary>
        void Delete(long userId);
    }
}