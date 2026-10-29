using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// Stores one profile photo per user as wwwroot/uploads/avatars/{userId}.{ext}.
    ///
    /// Design notes:
    /// - Filename is the user's ID, so "replace" is automatic: SaveAsync calls Delete(userId) first.
    /// - File type is detected from the first bytes (magic bytes), not from the client's
    ///   Content-Type or the filename extension — both are attacker-controlled.
    /// - GetUrl() appends "?v={lastWriteTime}" to defeat browser caching after a replace.
    /// - Only JPG, PNG and WebP are accepted (no GIF: animated avatars are usually a bad UX).
    /// </summary>
    public class ProfilePhotoService : IProfilePhotoService
    {
        private const long MaxBytes = 2 * 1024 * 1024;
        private static readonly string[] Extensions = { ".jpg", ".png", ".webp" };

        private readonly string _folder;

        public ProfilePhotoService(IWebHostEnvironment env)
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _folder = Path.Combine(webRoot, "uploads", "avatars");
            Directory.CreateDirectory(_folder);
        }

        /// <summary>
        /// Returns the URL of the user's photo, or null if they don't have one.
        /// The ?v= query string changes when the file changes, which forces the browser to re-fetch it.
        /// </summary>
        public string? GetUrl(long userId)
        {
            foreach (var ext in Extensions)
            {
                var path = Path.Combine(_folder, userId + ext);
                if (File.Exists(path))
                    return $"/uploads/avatars/{userId}{ext}?v={File.GetLastWriteTimeUtc(path).Ticks}";
            }
            return null;
        }

        /// <summary>
        /// Saves a new photo, replacing any existing one.
        /// Returns null on success, or an error message for the form.
        /// </summary>
        public async Task<string?> SaveAsync(long userId, IFormFile file)
        {
            if (file.Length == 0) return "Choose an image first.";
            if (file.Length > MaxBytes) return "The image is too large. The maximum size is 2 MB.";

            // Read just the first 12 bytes to detect the type.
            var header = new byte[12];
            int read;
            await using (var stream = file.OpenReadStream())
                read = await stream.ReadAsync(header, 0, header.Length);

            var ext = read == header.Length ? DetectExtension(header) : null;
            if (ext == null) return "Only JPG, PNG or WebP images are allowed.";

            // Replace: remove the previous file, whatever extension it had.
            Delete(userId);

            await using var fs = new FileStream(Path.Combine(_folder, userId + ext), FileMode.Create);
            await file.CopyToAsync(fs);
            return null;
        }

        public void Delete(long userId)
        {
            // File.Delete is a no-op when the file does not exist, so no "if exists" check is needed.
            foreach (var ext in Extensions)
                File.Delete(Path.Combine(_folder, userId + ext));
        }

        /// <summary>
        /// Magic-byte signature detection for the three supported formats.
        /// </summary>
        private static string? DetectExtension(byte[] h)
        {
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";
            if (h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
                h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";
            return null;
        }
    }
}
