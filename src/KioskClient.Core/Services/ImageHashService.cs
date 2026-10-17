using System.Security.Cryptography;

namespace KioskClient.Core.Services;

/// <summary>
/// Computes a content hash for an image referenced by URL or file path.
/// </summary>
public interface IImageHashService
{
    /// <summary>Returns a hash of the image content, or null if it could not be read.</summary>
    Task<string?> ComputeHashAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation that hashes local files and HTTP(S) resources with SHA-256.
/// </summary>
public class ImageHashService : IImageHashService
{
    private static readonly HttpClient SharedClient = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KioskClient/2.0");
        return client;
    }

    public async Task<string?> ComputeHashAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                using var response = await SharedClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            }

            var filePath = uri is { IsFile: true } ? uri.LocalPath : path;
            if (!File.Exists(filePath)) return null;

            await using var fileStream = File.OpenRead(filePath);
            return Convert.ToHexString(await SHA256.HashDataAsync(fileStream, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
