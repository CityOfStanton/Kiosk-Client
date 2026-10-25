using System.Security.Cryptography;
using System.Text;
using KioskClient.Core.Logging;
using KioskClient.Core.Models;
using KioskClient.Core.Serialization;

namespace KioskClient.Core.Services;

/// <summary>
/// Loads orchestrations from URLs or local files.
/// </summary>
public interface IOrchestrationLoader
{
    /// <summary>Loads an orchestration from a URL.</summary>
    Task<Orchestration> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Loads an orchestration from a local file path.</summary>
    Task<Orchestration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Loads an orchestration from raw content string.</summary>
    Orchestration LoadFromContent(string content);
}

/// <summary>
/// Default orchestration loader.
/// </summary>
public class OrchestrationLoader : IOrchestrationLoader
{
    private readonly IHttpService _httpService;

    public OrchestrationLoader(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<Orchestration> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid URL: {url}", nameof(url));

        var urlError = SecurityPolicy.CheckRemoteUrl(uri);
        if (urlError is not null)
            throw new InvalidOperationException(urlError);

        var content = await _httpService.GetStringAsync(uri, cancellationToken);
        EnsureOrchestrationSize(content);
        var orchestration = OrchestrationSerializer.Deserialize(content)
            ?? throw new InvalidOperationException("Failed to deserialize orchestration from URL content.");

        orchestration.Source = OrchestrationSource.URL;
        orchestration.SourcePath = url;
        orchestration.ContentHash = ComputeHash(content);
        AppLog.Debug("Loader", $"Loaded orchestration from {AppLog.Describe(url)}: bytes={content.Length}, actions={orchestration.Actions.Count}.");
        return orchestration;
    }

    public async Task<Orchestration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Orchestration file not found: {filePath}", filePath);

        var info = new FileInfo(filePath);
        if (info.Length > SecurityPolicy.MaxOrchestrationBytes)
            throw new InvalidOperationException($"Orchestration file exceeds the maximum size of {SecurityPolicy.MaxOrchestrationBytes} bytes.");

        string content;
        await using (var fs = File.OpenRead(filePath))
            content = await SecurityPolicy.ReadBoundedStringAsync(fs, SecurityPolicy.MaxOrchestrationBytes, cancellationToken);
        var orchestration = OrchestrationSerializer.Deserialize(content)
            ?? throw new InvalidOperationException($"Failed to deserialize orchestration from file: {filePath}");

        orchestration.Source = OrchestrationSource.File;
        orchestration.SourcePath = filePath;
        orchestration.ContentHash = ComputeHash(content);
        AppLog.Debug("Loader", $"Loaded orchestration from {AppLog.Describe(filePath)}: bytes={content.Length}, actions={orchestration.Actions.Count}.");
        return orchestration;
    }

    public Orchestration LoadFromContent(string content)    {
        EnsureOrchestrationSize(content);
        var orchestration = OrchestrationSerializer.Deserialize(content)
            ?? throw new InvalidOperationException("Failed to deserialize orchestration from content.");
        orchestration.ContentHash = ComputeHash(content);
        return orchestration;
    }

    private static void EnsureOrchestrationSize(string content)
    {
        if (Encoding.UTF8.GetByteCount(content) > SecurityPolicy.MaxOrchestrationBytes)
            throw new InvalidOperationException($"Orchestration exceeds the maximum size of {SecurityPolicy.MaxOrchestrationBytes} bytes.");
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }
}
