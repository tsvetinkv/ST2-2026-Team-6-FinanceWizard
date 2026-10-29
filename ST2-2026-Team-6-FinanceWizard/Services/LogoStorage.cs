using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Net;
using System.Net.Sockets;

namespace ST2_2026_Team_6_FinanceWizard.Services;

/// <summary>
/// Saves merchant logos to wwwroot/uploads/merchants.
///
/// Design notes:
/// - Handles BOTH uploads and remote links behind one interface (ILogoStorage),
///   so the MerchantController does not need to know the difference.
/// - File type is decided by magic bytes, never by the filename or the browser's Content-Type.
/// - Downloading from a link is protected against SSRF: the host is resolved to an IP
///   and refused if it points to a private / loopback / link-local address.
/// - HTTP redirects are NOT followed, because a redirect could hop to an internal address.
/// - All files are stored under a random GUID name, so the original (possibly unsafe) filename never touches disk.
/// </summary>
public sealed class LogoStorage : ILogoStorage
{
    public const long MaxBytes = 2 * 1024 * 1024; // 2 MB

    // Must match the name used in Program.cs when the HttpClient is registered
    public const string HttpClientName = "logo-download";

    private const string RelativeFolder = "uploads/merchants";
    private const string TooLargeMessage = "The logo must be 2 MB or smaller.";

    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;

    public LogoStorage(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory)
    {
        _environment = environment;
        _httpClientFactory = httpClientFactory;
    }

    private string FolderPath => Path.Combine(_environment.WebRootPath, "uploads", "merchants");

    /// <summary>
    /// Saves an uploaded file after checking size and file type.
    /// </summary>
    public async Task<LogoResult> SaveUploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0) return LogoResult.Fail("The selected file is empty.");
        if (file.Length > MaxBytes) return LogoResult.Fail(TooLargeMessage);

        await using var stream = file.OpenReadStream();
        var bytes = await ReadLimitedAsync(stream, cancellationToken);
        if (bytes == null) return LogoResult.Fail(TooLargeMessage);

        return await SaveBytesAsync(bytes, cancellationToken);
    }

    /// <summary>
    /// Downloads an image from a public web address and saves it locally.
    /// </summary>
    public async Task<LogoResult> DownloadFromLinkAsync(string link, CancellationToken cancellationToken)
    {
        // Only plain web addresses. The server must not be pointed at local or internal addresses.
        if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return LogoResult.Fail("Please enter a full web address that starts with https://");
        }

        // SSRF protection: refuse hosts that resolve to private / internal IPs.
        var addressCheck = await IsPublicAddressAsync(uri.Host, cancellationToken);
        if (addressCheck == AddressCheck.Private)
        {
            return LogoResult.Fail("That address points to a private or local network and cannot be used.");
        }
        if (addressCheck == AddressCheck.Unknown)
        {
            return LogoResult.Fail("That address could not be checked. Check the link or upload the photo instead.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.ParseAdd("image/*");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Redirects are not followed: a redirect could lead to an internal address
            if (!response.IsSuccessStatusCode)
            {
                return LogoResult.Fail(
                    $"The image could not be downloaded (the server answered {(int)response.StatusCode}). " +
                    "Check that the link opens the picture itself.");
            }

            // If the server tells us the size upfront, we can reject before downloading.
            if (response.Content.Headers.ContentLength is long length && length > MaxBytes)
            {
                return LogoResult.Fail("The image behind that link is larger than 2 MB.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var bytes = await ReadLimitedAsync(stream, cancellationToken);
            if (bytes == null) return LogoResult.Fail("The image behind that link is larger than 2 MB.");

            return await SaveBytesAsync(bytes, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The HttpClient timeout fired (not a user-initiated cancellation).
            return LogoResult.Fail("The image server took too long to answer. Try again or upload the photo instead.");
        }
        catch (HttpRequestException)
        {
            return LogoResult.Fail("The image could not be reached. Check the link or upload the photo instead.");
        }
    }

    /// <summary>
    /// Deletes a logo file, safely (only the file name is used, never the full path).
    /// </summary>
    public void Delete(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath)) return;

        // Only the file name is used, so the path can never point outside the logo folder
        var fileName = Path.GetFileName(logoPath);
        if (string.IsNullOrEmpty(fileName)) return;

        var fullPath = Path.Combine(FolderPath, fileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    private async Task<LogoResult> SaveBytesAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        // The type is decided by the file's first bytes, never by its name or the browser's claim
        var extension = DetectExtension(bytes);
        if (extension == null)
        {
            return LogoResult.Fail("The logo must be a PNG, JPG, GIF or WebP image.");
        }

        Directory.CreateDirectory(FolderPath);

        // A random name: the original file name is never used
        var fileName = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(FolderPath, fileName), bytes, cancellationToken);

        return LogoResult.Ok(RelativeFolder + "/" + fileName);
    }

    /// <summary>
    /// Reads the stream but returns null as soon as the total exceeds MaxBytes.
    /// This is what makes the "size check" safe even for chunked responses without Content-Length.
    /// </summary>
    private static async Task<byte[]?> ReadLimitedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxBytes) return null;
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        return buffer.ToArray();
    }

    /// <summary>
    /// Magic-byte signature detection: file type is confirmed by content, not by extension.
    /// </summary>
    private static string? DetectExtension(byte[] b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
        if (b.Length >= 6 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') return ".gif";
        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
        return null;
    }

    private enum AddressCheck { Public, Private, Unknown }

    /// <summary>
    /// SSRF guard: returns Private if the host is (or resolves to) a non-public address.
    /// </summary>
    private static async Task<AddressCheck> IsPublicAddressAsync(string host, CancellationToken cancellationToken)
    {
        var literal = host.Trim('[', ']'); // allow IPv6 literals such as [::1]
        if (IPAddress.TryParse(literal, out var address))
        {
            return IsPrivate(address) ? AddressCheck.Private : AddressCheck.Public;
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
            if (addresses.Length == 0) return AddressCheck.Unknown;
            // If ANY of the resolved addresses is private, refuse — prevents DNS rebinding tricks.
            return addresses.Any(IsPrivate) ? AddressCheck.Private : AddressCheck.Public;
        }
        catch (SocketException)
        {
            return AddressCheck.Unknown;
        }
    }

    /// <summary>
    /// Returns true for loopback, private, link-local, CGNAT and multicast IPs.
    /// </summary>
    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return true;

        var b = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Any)) return true;
            if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80) return true; // fe80::/10 link-local
            if ((b[0] & 0xFE) == 0xFC) return true;                  // fc00::/7 unique local
            return false;
        }

        return b[0] == 0                                   // 0.0.0.0/8
            || b[0] == 10                                  // 10.0.0.0/8
            || b[0] == 127                                 // loopback
            || (b[0] == 169 && b[1] == 254)                // link-local
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)   // 172.16.0.0/12
            || (b[0] == 192 && b[1] == 168)                // 192.168.0.0/16
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)  // carrier-grade NAT
            || b[0] >= 224;                                // multicast and reserved
    }
}
