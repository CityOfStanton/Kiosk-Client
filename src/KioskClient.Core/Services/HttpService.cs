using System.Net;
using KioskClient.Core.Models;

namespace KioskClient.Core.Services;

/// <summary>
/// Provides HTTP operations for loading orchestration content.
/// </summary>
public interface IHttpService
{
    /// <summary>Fetches content from a URI.</summary>
    Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>Validates that a URI returns the expected HTTP status.</summary>
    Task<ValidationResult> ValidateUriAsync(string uri, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation using HttpClient.
/// </summary>
public class HttpService : IHttpService, IDisposable
{
    private readonly HttpClient _client;

    public HttpService()
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("KioskClient/2.0");
    }

    public HttpService(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<ValidationResult> ValidateUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult
        {
            Identifier = uri
        };

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
        {
            result.IsValid = false;
            result.Message = "Invalid URI format.";
            result.Guidance = "Ensure the URI is a valid absolute URL (e.g., https://example.com/file.json).";
            return result;
        }

        if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            result.IsValid = false;
            result.Message = "URI must use HTTP or HTTPS scheme.";
            result.Guidance = "Change the URI scheme to http:// or https://.";
            return result;
        }

        try
        {
            var response = await _client.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, parsedUri),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                result.IsValid = true;
                result.Message = "URI is reachable.";
            }
            else
            {
                // Some servers don't support HEAD, try GET
                response = await _client.GetAsync(parsedUri, cancellationToken);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.IsValid = true;
                    result.Message = "URI is reachable.";
                }
                else
                {
                    result.IsValid = false;
                    result.Message = $"URI returned HTTP {(int)response.StatusCode} ({response.StatusCode}).";
                    result.Guidance = "Verify the URI is correct and the server is accessible.";
                }
            }
        }
        catch (HttpRequestException ex)
        {
            result.IsValid = false;
            result.Message = $"Failed to reach URI: {ex.Message}";
            result.Guidance = "Check network connectivity and verify the URI is correct.";
        }
        catch (TaskCanceledException)
        {
            result.IsValid = false;
            result.Message = "Request timed out.";
            result.Guidance = "The server took too long to respond. Try again or check the URI.";
        }

        return result;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
