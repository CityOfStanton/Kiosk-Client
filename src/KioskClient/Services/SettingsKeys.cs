namespace KioskClient.Services;

/// <summary>
/// Constants for application settings keys.
/// </summary>
public static class SettingsKeys
{
    public const string OrchestrationSource = "OrchestrationSource";
    public const string OrchestrationUri = "OrchestrationUri";
    public const string DoNotShowTutorial = "DoNotShowTutorialOnStartup";
    public const string AutoRetryEnabled = "AutoRetryEnabled";
    public const string RetryTimeoutSeconds = "RetryTimeoutSeconds";
    public const string MaxUrlHistory = "MaxUrlHistory";
    public const string UrlHistory = "UrlHistory";
    public const string SavedOrchestration = "SavedOrchestration";
    public const string OrchestrationFilePath = "OrchestrationFilePath";

    /// <summary>Default retry timeout in seconds.</summary>
    public const int DefaultRetryTimeoutSeconds = 30;

    /// <summary>Default number of URL history entries to keep.</summary>
    public const int DefaultMaxUrlHistory = 5;
}
