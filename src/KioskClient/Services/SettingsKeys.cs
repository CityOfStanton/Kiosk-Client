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
    public const string BlockHttp = "BlockHttp";
    public const string MaxContentMegabytes = "MaxContentMegabytes";
    public const int DefaultMaxContentMegabytes = 50;
    public const string RetryTimeoutSeconds = "RetryTimeoutSeconds";
    public const string MaxUrlHistory = "MaxUrlHistory";
    public const string UrlHistory = "UrlHistory";
    public const string SavedOrchestration = "SavedOrchestration";
    public const string OrchestrationFilePath = "OrchestrationFilePath";
    public const string LastRunSource = "LastRunSource";
    public const string LastRunPath = "LastRunPath";
    public const string ShowCountdown = "ShowCountdown";
    public const string ShowNetworkWatermark = "ShowNetworkWatermark";
    public const string SettingsButtonOpacityPercent = "SettingsButtonOpacityPercent";

    /// <summary>Default overlay Settings button opacity percentage.</summary>
    public const int DefaultSettingsButtonOpacityPercent = 50;
    public const string NetworkWatermarkBackground = "NetworkWatermarkBackground";
    public const string NetworkWatermarkForeground = "NetworkWatermarkForeground";

    /// <summary>Default network indicator background color (ARGB hex).</summary>
    public const string DefaultNetworkWatermarkBackground = "#FFFF0000";

    /// <summary>Default network indicator font color (ARGB hex).</summary>
    public const string DefaultNetworkWatermarkForeground = "#FF000000";
    public const string CountdownColor = "CountdownColor";
    public const string CountdownOpacityPercent = "CountdownOpacityPercent";
    public const string CountdownAutoContrast = "CountdownAutoContrast";

    /// <summary>Default countdown font color (ARGB hex).</summary>
    public const string DefaultCountdownColor = "#FFFFFFFF";

    /// <summary>Default countdown opacity percentage.</summary>
    public const int DefaultCountdownOpacityPercent = 25;

    /// <summary>Default retry timeout in seconds.</summary>
    public const int DefaultRetryTimeoutSeconds = 30;

    /// <summary>Default number of URL history entries to keep.</summary>
    public const int DefaultMaxUrlHistory = 5;
}
