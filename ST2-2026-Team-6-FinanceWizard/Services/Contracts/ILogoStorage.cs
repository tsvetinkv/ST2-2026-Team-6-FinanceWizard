namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

public interface ILogoStorage
{
    // Saves an uploaded photo. The result holds either the stored path or a message for the user.
    Task<LogoResult> SaveUploadAsync(IFormFile file, CancellationToken cancellationToken);

    // Downloads the image behind a web link and saves it the same way as an upload.
    Task<LogoResult> DownloadFromLinkAsync(string link, CancellationToken cancellationToken);

    // Removes a logo file that was saved by this class (anything else is ignored).
    void Delete(string? logoPath);
}

public sealed record LogoResult(string? RelativePath, string? Error)
{
    public bool Succeeded => Error == null;

    public static LogoResult Ok(string relativePath) => new(relativePath, null);

    public static LogoResult Fail(string error) => new(null, error);
}
