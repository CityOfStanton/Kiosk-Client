namespace KioskClient.Core.Models;

/// <summary>
/// Defines the source of an orchestration file.
/// </summary>
public enum OrchestrationSource
{
    /// <summary>Orchestration loaded from a URL.</summary>
    URL,

    /// <summary>Orchestration loaded from a local file.</summary>
    File
}
