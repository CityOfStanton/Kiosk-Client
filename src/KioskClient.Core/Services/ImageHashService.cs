using System.Security.Cryptography;
using KioskClient.Core.Logging;

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
                if (SecurityPolicy.CheckRemoteUrl(uri) is not null) return null;

                using var response = await SharedClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > SecurityPolicy.MaxContentBytes) return null;
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await HashBoundedAsync(stream, cancellationToken);
            }

            if (uri is { IsFile: false }) return null;
            var filePath = uri is { IsFile: true } ? uri.LocalPath : path;
            if (!File.Exists(filePath)) return null;
            if (new FileInfo(filePath).Length > SecurityPolicy.MaxContentBytes) return null;

            await using var fileStream = File.OpenRead(filePath);
            return await HashBoundedAsync(fileStream, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Warn("ImageHash", $"Could not hash image {AppLog.Describe(path)}; it will be reloaded.", ex);
            return null;
        }
    }

    private static async Task<string?> HashBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > SecurityPolicy.MaxContentBytes) return null;
            sha.AppendData(buffer, 0, read);
        }
        return Convert.ToHexString(sha.GetHashAndReset());
    }
}
