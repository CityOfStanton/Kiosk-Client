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

        var content = await _httpService.GetStringAsync(uri, cancellationToken);
        var orchestration = OrchestrationSerializer.Deserialize(content)
            ?? throw new InvalidOperationException("Failed to deserialize orchestration from URL content.");

        orchestration.Source = OrchestrationSource.URL;
        orchestration.SourcePath = url;
        return orchestration;
    }

    public async Task<Orchestration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Orchestration file not found: {filePath}", filePath);

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var orchestration = OrchestrationSerializer.Deserialize(content)
            ?? throw new InvalidOperationException($"Failed to deserialize orchestration from file: {filePath}");

        orchestration.Source = OrchestrationSource.File;
        orchestration.SourcePath = filePath;
        return orchestration;
    }

    public Orchestration LoadFromContent(string content)
    {
        return OrchestrationSerializer.Deserialize(content)
            ?? throw new InvalidOperationException("Failed to deserialize orchestration from content.");
    }
}
