namespace KioskClient.Core.Models;

/// <summary>
/// Defines how the orchestration behaves after completing all actions.
/// </summary>
public enum LifecycleBehavior
{
    /// <summary>Exit after all actions have been displayed once.</summary>
    SingleRun,

    /// <summary>Restart from the first action after completing.</summary>
    ContinuousLoop
}
