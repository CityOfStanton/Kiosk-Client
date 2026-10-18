using KioskClient.Core.Models;

namespace KioskClient.Core.Services;

/// <summary>
/// Converts orchestrations authored in older formats to the current (2.0) format.
/// </summary>
public static class OrchestrationMigrator
{
    /// <summary>The orchestration format version written by this application.</summary>
    public const string CurrentVersion = "2.0";

    private static readonly Version Current = new(2, 0);

    /// <summary>Returns true when the orchestration uses an older version or deprecated fields.</summary>
    public static bool NeedsMigration(Orchestration orchestration) =>
        orchestration.UsedDeprecatedPollingField
        || orchestration.Actions.Any(a => a.GetDeprecatedFields().Count > 0)
        || IsOutdated(orchestration.Version)
        || orchestration.Actions.Any(a => IsOutdated(a.Version));

    /// <summary>
    /// Upgrades the orchestration in place to the current version.
    /// </summary>
    /// <returns>A human-readable list of the changes that were made (empty if none).</returns>
    public static List<string> Migrate(Orchestration orchestration)
    {
        var changes = new List<string>();

        if (IsOutdated(orchestration.Version))
        {
            var old = string.IsNullOrWhiteSpace(orchestration.Version) ? "(none)" : orchestration.Version;
            changes.Add($"Orchestration version updated from {old} to {CurrentVersion}.");
            orchestration.Version = CurrentVersion;
        }

        if (orchestration.UsedDeprecatedPollingField)
        {
            changes.Add("Removed deprecated 'pollingInterval' / 'pollingIntervalMinutes' field. Polling is now derived from action durations.");
            orchestration.UsedDeprecatedPollingField = false;
        }

        foreach (var action in orchestration.Actions)
            changes.AddRange(action.RemoveDeprecatedFields());

        var updatedActions = 0;
        foreach (var action in orchestration.Actions.Where(a => IsOutdated(a.Version)))
        {
            action.Version = CurrentVersion;
            updatedActions++;
        }

        if (updatedActions > 0)
            changes.Add($"Updated {updatedActions} action(s) to version {CurrentVersion}.");

        return changes;
    }

    private static bool IsOutdated(string? version) =>
        !Version.TryParse(version, out var parsed) || parsed < Current;
}
