using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// Stores one profile photo per user as wwwroot/uploads/avatars/{userId}.{ext}.
    /// The file type is checked by its first bytes, not by the file name.
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

        public async Task<string?> SaveAsync(long userId, IFormFile file)
        {
            if (file.Length == 0) return "Choose an image first.";
            if (file.Length > MaxBytes) return "The image is too large. The maximum size is 2 MB.";

            var header = new byte[12];
            int read;
            await using (var stream = file.OpenReadStream())
                read = await stream.ReadAsync(header, 0, header.Length);

            var ext = read == header.Length ? DetectExtension(header) : null;
            if (ext == null) return "Only JPG, PNG or WebP images are allowed.";

            Delete(userId); // remove the previous photo (it may have another extension)

            await using var fs = new FileStream(Path.Combine(_folder, userId + ext), FileMode.Create);
            await file.CopyToAsync(fs);
            return null;
        }

        public void Delete(long userId)
        {
            foreach (var ext in Extensions)
                File.Delete(Path.Combine(_folder, userId + ext)); // no error if missing
        }

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