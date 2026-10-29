namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

/// <summary>
/// Contract for saving / deleting merchant logos.
///
/// Design pattern: ABSTRACTION over file-system and HTTP concerns.
/// - The controller calls SaveUploadAsync for uploads and DownloadFromLinkAsync for links.
/// - The concrete LogoStorage hides: MIME type detection, size limits, SSRF protection,
///   random file naming, redirect policies.
/// - A future implementation could store logos in blob storage (S3, Azure Blob) without changing
///   the controller or the Merchant entity.
/// </summary>
public interface ILogoStorage
{
    /// <summary>
    /// Saves an uploaded photo. The result holds either the stored path or a message for the user.
    /// </summary>
    Task<LogoResult> SaveUploadAsync(IFormFile file, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the image behind a web link and saves it the same way as an upload.
    /// Must validate the URL and refuse local / private addresses (SSRF protection).
    /// </summary>
    Task<LogoResult> DownloadFromLinkAsync(string link, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a logo file that was saved by this class (anything else is ignored).
    /// </summary>
    void Delete(string? logoPath);
}

/// <summary>
/// Result of a logo operation.
///
/// Design notes:
/// - A record: immutable, value-based equality, small and cheap.
/// - Instead of throwing on failure, the implementation returns a Fail(error) result
///   so the controller can add the error to ModelState without a try/catch per call.
/// - Succeeded is a derived helper so consumers can write "if (result.Succeeded)".
/// </summary>
public sealed record LogoResult(string? RelativePath, string? Error)
{
    public bool Succeeded => Error == null;

    public static LogoResult Ok(string relativePath) => new(relativePath, null);

    public static LogoResult Fail(string error) => new(null, error);
}
