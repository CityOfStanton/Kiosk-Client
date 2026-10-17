namespace KioskClient.Core.Services;

/// <summary>
/// Central security limits and path/URL rules shared by loading, validation and content fetching.
/// </summary>
public static class SecurityPolicy
{
    /// <summary>Hard cap on the size of an orchestration file (1 MB). Not configurable.</summary>
    public const int MaxOrchestrationBytes = 1024 * 1024;

    /// <summary>Default cap on downloaded/hashed content (50 MB).</summary>
    public const long DefaultMaxContentBytes = 50L * 1024 * 1024;

    /// <summary>Maximum allowed length of a path or URL.</summary>
    public const int MaxPathLength = 2048;

    /// <summary>Maximum allowed duration/delay in seconds (24 hours).</summary>
    public const int MaxSeconds = 86400;

    /// <summary>When true (default), http:// addresses are rejected.</summary>
    public static bool BlockHttp { get; set; } = true;

    /// <summary>Configurable cap on content size in bytes.</summary>
    public static long MaxContentBytes { get; set; } = DefaultMaxContentBytes;

    /// <summary>
    /// Checks a content path or URL. Returns an error message, or null when acceptable.
    /// Network-sourced orchestrations (<paramref name="isRemoteSource"/>) may only reference remote URLs.
    /// </summary>
    public static string? CheckPath(string? path, bool isRemoteSource)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Path is required.";
        if (path.Length > MaxPathLength) return $"Path exceeds {MaxPathLength} characters.";
        if (path.Any(char.IsControl)) return "Path contains control characters.";

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttps) return null;

            if (uri.Scheme == Uri.UriSchemeHttp)
                return BlockHttp ? "HTTP addresses are blocked. Use HTTPS or disable the HTTP block in Settings." : null;

            if (uri.IsFile)
            {
                if (isRemoteSource) return "Orchestrations loaded from the network cannot reference local or network-share paths.";
                if (uri.IsUnc) return "Network share (UNC) paths are not allowed.";
                return null;
            }

            return $"Unsupported URI scheme '{uri.Scheme}'.";
        }

        if (isRemoteSource) return "Orchestrations loaded from the network cannot reference local paths.";
        if (!Path.IsPathRooted(path)) return "Local paths must be absolute.";
        return null;
    }

    /// <summary>Checks a URL for use as a network request. Returns an error message or null.</summary>
    public static string? CheckRemoteUrl(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps) return null;
        if (uri.Scheme == Uri.UriSchemeHttp)
            return BlockHttp ? "HTTP addresses are blocked. Use HTTPS or disable the HTTP block in Settings." : null;
        return "URI must use HTTP or HTTPS scheme.";
    }

    /// <summary>Reads a stream as UTF-8 text, throwing if it exceeds <paramref name="maxBytes"/>.</summary>
    public static async Task<string> ReadBoundedStringAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (ms.Length + read > maxBytes)
                throw new InvalidOperationException($"Content exceeds the maximum allowed size of {maxBytes} bytes.");
            ms.Write(buffer, 0, read);
        }
        return System.Text.Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }
}
