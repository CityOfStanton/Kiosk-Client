using System.Net;
using KioskClient.Core.Logging;
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
        var error = SecurityPolicy.CheckRemoteUrl(uri);
        if (error is not null)
            throw new InvalidOperationException(error);

        using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        AppLog.Debug("Http", $"GET {AppLog.Describe(uri.ToString())} -> {(int)response.StatusCode} {response.StatusCode}, contentLength={response.Content.Headers.ContentLength?.ToString() ?? "unknown"}.");
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > SecurityPolicy.MaxOrchestrationBytes)
            throw new InvalidOperationException($"Orchestration exceeds the maximum size of {SecurityPolicy.MaxOrchestrationBytes} bytes.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await SecurityPolicy.ReadBoundedStringAsync(stream, SecurityPolicy.MaxOrchestrationBytes, cancellationToken);
    }

    public async Task<ValidationResult> ValidateUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult
        {
            Identifier = uri
        };

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
        {
            AppLog.Warn("Http", "URL validation failed: invalid URI format.");
            result.IsValid = false;
            result.Message = "Invalid URI format.";
            result.Guidance = "Ensure the URI is a valid absolute URL (e.g., https://example.com/file.json).";
            return result;
        }

        var policyError = SecurityPolicy.CheckRemoteUrl(parsedUri);
        if (policyError is not null)
        {
            AppLog.Warn("Http", $"URL rejected by security policy: {AppLog.Describe(uri)}. Reason: {policyError}");
            result.IsValid = false;
            result.Message = policyError;
            result.Guidance = "Use an https:// address.";
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
                response = await _client.GetAsync(parsedUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
                    AppLog.Warn("Http", $"URL validation failed for {AppLog.Describe(uri)}: HTTP {(int)response.StatusCode} ({response.StatusCode}).");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            AppLog.Warn("Http", $"URL validation failed for {AppLog.Describe(uri)}: request error.", ex);
            result.IsValid = false;
            result.Message = $"Failed to reach URI: {ex.Message}";
            result.Guidance = "Check network connectivity and verify the URI is correct.";
        }
        catch (TaskCanceledException ex)
        {
            AppLog.Warn("Http", $"URL validation timed out or was cancelled for {AppLog.Describe(uri)}.", ex);
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
