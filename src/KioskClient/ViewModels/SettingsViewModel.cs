using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KioskClient.Core.Logging;
using KioskClient.Core.Models;
using KioskClient.Core.Services;
using KioskClient.Services;

namespace KioskClient.ViewModels;

/// <summary>
/// Outcome of attempting to auto-start the last orchestration on application startup.
/// </summary>
public enum StartupLoadResult
{
    NoPreviousOrchestration,
    LoadedAndValid,
    LoadFailed,
}

/// <summary>
/// ViewModel for the Settings page. Manages orchestration loading, validation,
/// URL history, auto-retry, and all settings-related state.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IOrchestrationLoader _loader;
    private readonly IHttpService _httpService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadUri))]
    [NotifyPropertyChangedFor(nameof(IsFileMode))]
    private bool _isLocalFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadUri))]
    private string _uriPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadFile))]
    private string _localPath = string.Empty;

    [ObservableProperty]
    private bool? _isUriPathVerified;

    [ObservableProperty]
    private bool? _isLocalPathVerified;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOrchestrationLoaded))]
    [NotifyPropertyChangedFor(nameof(IsOrchestrationValid))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(RunTooltip))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryName))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryVersion))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummarySource))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryLifecycle))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryOrder))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryActionCount))]
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryRuntime))]
    [NotifyPropertyChangedFor(nameof(HasDeprecatedFields))]
    [NotifyPropertyChangedFor(nameof(DeprecatedFieldsMessage))]
    private Orchestration? _orchestration;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(RunTooltip))]
    private ValidationResult? _orchestrationValidationResult;

    [ObservableProperty]
    private string? _loadError;

    [ObservableProperty]
    private bool _isUriLoading;

    [ObservableProperty]
    private bool _isFileLoading;

    [ObservableProperty]
    private bool _isAutoRetryEnabled;

    [ObservableProperty]
    private bool _blockHttp = true;

    [ObservableProperty]
    private double _maxContentMegabytes = SettingsKeys.DefaultMaxContentMegabytes;

    partial void OnBlockHttpChanged(bool value)
    {
        SecurityPolicy.BlockHttp = value;
        _settings.SaveSetting(SettingsKeys.BlockHttp, value);
    }

    partial void OnMaxContentMegabytesChanged(double value)
    {
        if (double.IsNaN(value) || value < 1) return;
        SecurityPolicy.MaxContentBytes = (long)value * 1024 * 1024;
        _settings.SaveSetting(SettingsKeys.MaxContentMegabytes, (int)value);
    }

    [ObservableProperty]
    private int _autoRetrySeconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoRetryCountdownDisplay))]
    private int _currentAutoRetryCountdown;

    [ObservableProperty]
    private bool _isAutoRetryActive;

    [ObservableProperty]
    private bool _shouldAutoRetryStart;

    [ObservableProperty]
    private double _maxUrlHistory = SettingsKeys.DefaultMaxUrlHistory;

    partial void OnMaxUrlHistoryChanged(double value)
    {
        if (double.IsNaN(value)) return;

        var max = (int)Math.Clamp(Math.Round(value), 1, 50);
        _settings.SaveSetting(SettingsKeys.MaxUrlHistory, max);

        var trimmed = false;
        while (UrlHistory.Count > max)
        {
            UrlHistory.RemoveAt(UrlHistory.Count - 1);
            trimmed = true;
        }

        if (trimmed)
            _settings.SaveSetting(SettingsKeys.UrlHistory, UrlHistory.ToList());
    }

    [ObservableProperty]
    private bool _showCountdown;

    [ObservableProperty]
    private Windows.UI.Color _countdownColor = Microsoft.UI.Colors.White;

    [ObservableProperty]
    private bool _countdownAutoContrast = true;

    partial void OnCountdownAutoContrastChanged(bool value) => _settings.SaveSetting(SettingsKeys.CountdownAutoContrast, value);

    [ObservableProperty]
    private double _countdownOpacityPercent = SettingsKeys.DefaultCountdownOpacityPercent;
    [ObservableProperty]
    private double _settingsButtonOpacityPercent = SettingsKeys.DefaultSettingsButtonOpacityPercent;

    partial void OnSettingsButtonOpacityPercentChanged(double value)
    {
        if (double.IsNaN(value)) return;
        _settings.SaveSetting(SettingsKeys.SettingsButtonOpacityPercent, (int)Math.Round(Math.Clamp(value, 0, 100)));
    }

    public double SettingsButtonOpacity => Math.Clamp(SettingsButtonOpacityPercent, 0, 100) / 100.0;

    [ObservableProperty]
    private bool _showNetworkWatermark = true;

    partial void OnShowNetworkWatermarkChanged(bool value) => _settings.SaveSetting(SettingsKeys.ShowNetworkWatermark, value);

    [ObservableProperty]
    private Windows.UI.Color _networkWatermarkBackground = Microsoft.UI.Colors.Red;

    [ObservableProperty]
    private Windows.UI.Color _networkWatermarkForeground = Microsoft.UI.Colors.Black;

    partial void OnNetworkWatermarkBackgroundChanged(Windows.UI.Color value) =>
        _settings.SaveSetting(SettingsKeys.NetworkWatermarkBackground, $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");

    partial void OnNetworkWatermarkForegroundChanged(Windows.UI.Color value) =>
        _settings.SaveSetting(SettingsKeys.NetworkWatermarkForeground, $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");

    partial void OnShowCountdownChanged(bool value) => _settings.SaveSetting(SettingsKeys.ShowCountdown, value);

    partial void OnCountdownColorChanged(Windows.UI.Color value) =>
        _settings.SaveSetting(SettingsKeys.CountdownColor, $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");

    partial void OnCountdownOpacityPercentChanged(double value)
    {
        if (double.IsNaN(value)) return;
        _settings.SaveSetting(SettingsKeys.CountdownOpacityPercent, (int)Math.Round(Math.Clamp(value, 0, 100)));
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        var s = hex.TrimStart('#');
        if (s.Length == 8 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v))
            return Windows.UI.Color.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
        return Microsoft.UI.Colors.White;
    }

    public ObservableCollection<string> UrlHistory { get; } = [];
    public ObservableCollection<string> LogEntries { get; } = [];

    // Computed properties
    public bool IsFileMode => IsLocalFile;
    public bool IsOrchestrationLoaded => Orchestration is not null;
    public bool IsOrchestrationValid => OrchestrationValidationResult?.IsFullyValid == true;
    public bool CanStart => IsOrchestrationLoaded && IsOrchestrationValid;

    public string RunTooltip
    {
        get
        {
            const string baseTooltip = "Start orchestration (F5 or Ctrl+R)";
            if (!IsOrchestrationLoaded)
                return $"{baseTooltip} — No orchestration loaded";
            if (!IsOrchestrationValid)
                return $"{baseTooltip} — Orchestration has validation errors";
            return baseTooltip;
        }
    }
    public bool CanLoadUri => !IsLocalFile && !string.IsNullOrWhiteSpace(UriPath) && !IsUriLoading;
    public bool CanLoadFile => IsLocalFile && !string.IsNullOrWhiteSpace(LocalPath) && !IsFileLoading;

    // Summary properties
    public string OrchestrationSummaryName => Orchestration?.Name ?? "N/A";
    public string OrchestrationSummaryVersion => Orchestration?.Version ?? "N/A";
    public string OrchestrationSummarySource => Orchestration?.Source.ToString() ?? "N/A";
    public string OrchestrationSummaryLifecycle => Orchestration?.Lifecycle.ToString() ?? "N/A";
    public string OrchestrationSummaryOrder => Orchestration?.Order.ToString() ?? "N/A";
    public int OrchestrationSummaryActionCount => Orchestration?.Actions.Count ?? 0;
    public string OrchestrationSummaryRuntime => Orchestration is not null
        ? FormatTimeSpan(Orchestration.TotalRuntime)
        : "N/A";
    public bool HasDeprecatedFields => DeprecatedFieldNames().Any();

    public string DeprecatedFieldsMessage =>
        $"This orchestration contains deprecated fields that are no longer used: {string.Join(", ", DeprecatedFieldNames())}. Open it in the Editor to convert it automatically. These fields will not be written when you save the file.";

    private IEnumerable<string> DeprecatedFieldNames()
    {
        if (Orchestration is null) yield break;

        if (Orchestration.UsedDeprecatedPollingField)
            yield return "pollingInterval / pollingIntervalMinutes";

        foreach (var action in Orchestration.Actions)
        {
            var fields = action.GetDeprecatedFields();
            if (fields.Count > 0)
                yield return $"{string.Join(", ", fields)} (in '{action.Name}')";
        }
    }
    public string AutoRetryCountdownDisplay => $"{CurrentAutoRetryCountdown}s";

    public SettingsViewModel(ISettingsService settings, IOrchestrationLoader loader, IHttpService httpService)
    {
        _settings = settings;
        _loader = loader;
        _httpService = httpService;
        MigrateLegacySettings();
        try
        {
            LoadState();
        }
        catch (Exception ex)
        {
            AppLog.Error("Settings", "Failed to load saved settings; falling back to defaults.", ex);
            RecoverPaths();
        }
    }

    private const string LegacyDefaultUriKey = "#DefaultOrchestrationURI";
    private const string LegacyDefaultSourceKey = "#DefaultOrchestrationSource";

    /// <summary>
    /// Carries the previous version's default orchestration path over to the current settings keys.
    /// Never throws.
    /// </summary>
    private void MigrateLegacySettings()
    {
        try
        {
            var legacyPath = _settings.GetSetting<string>(LegacyDefaultUriKey);
            if (string.IsNullOrWhiteSpace(legacyPath)) return;

            var isFile = string.Equals(_settings.GetSetting<string>(LegacyDefaultSourceKey), "File", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(_settings.GetSetting<string>(SettingsKeys.LastRunPath)))
            {
                _settings.SaveSetting(SettingsKeys.LastRunSource, isFile ? "File" : "URL");
                _settings.SaveSetting(SettingsKeys.LastRunPath, legacyPath);
            }

            var hasCurrent = !string.IsNullOrWhiteSpace(_settings.GetSetting<string>(SettingsKeys.OrchestrationUri))
                || !string.IsNullOrWhiteSpace(_settings.GetSetting<string>(SettingsKeys.OrchestrationFilePath));
            if (hasCurrent) return;

            _settings.SaveSetting(SettingsKeys.OrchestrationSource, isFile ? "File" : "URL");
            _settings.SaveSetting(isFile ? SettingsKeys.OrchestrationFilePath : SettingsKeys.OrchestrationUri, legacyPath);
            AppLog.Info("Settings", $"Migrated legacy default orchestration (source={(isFile ? "File" : "URL")}).");
        }
        catch (Exception ex)
        {
            AppLog.Warn("Settings", "Legacy settings migration failed.", ex);
        }
    }

    /// <summary>
    /// Fail-safe used when saved state
    /// </summary>
    private void RecoverPaths()
    {
        try
        {
            var lastSource = _settings.GetSetting<string>(SettingsKeys.LastRunSource);
            var lastPath = _settings.GetSetting<string>(SettingsKeys.LastRunPath);
            var uri = _settings.GetSetting<string>(SettingsKeys.OrchestrationUri);
            var file = _settings.GetSetting<string>(SettingsKeys.OrchestrationFilePath);

            if (lastSource == "File" && !string.IsNullOrEmpty(lastPath)) file = lastPath;
            else if (!string.IsNullOrEmpty(lastPath)) uri = lastPath;

            IsLocalFile = lastSource == "File" || (string.IsNullOrEmpty(uri) && !string.IsNullOrEmpty(file));
            UriPath = uri ?? string.Empty;
            LocalPath = file ?? string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error("Settings", "Path recovery failed.", ex);
        }
    }

    /// <summary>
    /// Loads saved state from application storage.
    /// </summary>
    private void LoadState()
    {
        IsAutoRetryEnabled = _settings.GetSetting(SettingsKeys.AutoRetryEnabled, true);
        AutoRetrySeconds = _settings.GetSetting(SettingsKeys.RetryTimeoutSeconds, SettingsKeys.DefaultRetryTimeoutSeconds);
        BlockHttp = _settings.GetSetting(SettingsKeys.BlockHttp, true);
        SecurityPolicy.BlockHttp = BlockHttp;
        MaxContentMegabytes = Math.Clamp(_settings.GetSetting(SettingsKeys.MaxContentMegabytes, SettingsKeys.DefaultMaxContentMegabytes), 1, 1024);
        SecurityPolicy.MaxContentBytes = (long)MaxContentMegabytes * 1024 * 1024;

        ShowCountdown = _settings.GetSetting(SettingsKeys.ShowCountdown, false);
        ShowNetworkWatermark = _settings.GetSetting(SettingsKeys.ShowNetworkWatermark, true);
        SettingsButtonOpacityPercent = _settings.GetSetting(SettingsKeys.SettingsButtonOpacityPercent, SettingsKeys.DefaultSettingsButtonOpacityPercent);
        NetworkWatermarkBackground = ParseColor(_settings.GetSetting(SettingsKeys.NetworkWatermarkBackground, SettingsKeys.DefaultNetworkWatermarkBackground) ?? SettingsKeys.DefaultNetworkWatermarkBackground);
        NetworkWatermarkForeground = ParseColor(_settings.GetSetting(SettingsKeys.NetworkWatermarkForeground, SettingsKeys.DefaultNetworkWatermarkForeground) ?? SettingsKeys.DefaultNetworkWatermarkForeground);
        CountdownColor = ParseColor(_settings.GetSetting(SettingsKeys.CountdownColor, SettingsKeys.DefaultCountdownColor) ?? SettingsKeys.DefaultCountdownColor);
        CountdownOpacityPercent = _settings.GetSetting(SettingsKeys.CountdownOpacityPercent, SettingsKeys.DefaultCountdownOpacityPercent);
        CountdownAutoContrast = _settings.GetSetting(SettingsKeys.CountdownAutoContrast, true);

        var maxHistory = _settings.GetSetting(SettingsKeys.MaxUrlHistory, SettingsKeys.DefaultMaxUrlHistory);
        MaxUrlHistory = maxHistory;
        var historyList = _settings.GetSetting<List<string>>(SettingsKeys.UrlHistory);
        if (historyList is not null)
        {
            foreach (var url in historyList.Take(maxHistory))
                UrlHistory.Add(url);
        }

        var savedSource = _settings.GetSetting<string>(SettingsKeys.OrchestrationSource);
        IsLocalFile = savedSource == "File";

        UriPath = _settings.GetSetting<string>(SettingsKeys.OrchestrationUri) ?? string.Empty;
        LocalPath = _settings.GetSetting<string>(SettingsKeys.OrchestrationFilePath) ?? string.Empty;
    }

    /// <summary>
    /// Saves current state to application storage.
    /// </summary>
    public void SaveState()
    {
        _settings.SaveSetting(SettingsKeys.AutoRetryEnabled, IsAutoRetryEnabled);
        _settings.SaveSetting(SettingsKeys.RetryTimeoutSeconds, AutoRetrySeconds);
        _settings.SaveSetting(SettingsKeys.OrchestrationSource, IsLocalFile ? "File" : "URL");
        _settings.SaveSetting(SettingsKeys.OrchestrationUri, UriPath);
        _settings.SaveSetting(SettingsKeys.OrchestrationFilePath, LocalPath);
        _settings.SaveSetting(SettingsKeys.UrlHistory, UrlHistory.ToList());
    }

    [RelayCommand]
    private async Task LoadFromUriAsync()
    {
        if (string.IsNullOrWhiteSpace(UriPath)) return;

        IsUriLoading = true;
        IsUriPathVerified = null;
        Orchestration = null;
        OrchestrationValidationResult = null;
        LoadError = null;
        AddLog($"Loading orchestration from: {UriPath}");
        AppLog.Info("Settings", $"Loading orchestration from {AppLog.Describe(UriPath)}.");

        try
        {
            // Validate URI first
            var validation = await _httpService.ValidateUriAsync(UriPath);
            if (validation.IsValid != true)
            {
                IsUriPathVerified = false;
                AddLog($"URI validation failed: {validation.Message}");
                AppLog.Warn("Settings", $"URI validation failed: {validation.Message}");
                IsUriLoading = false;
                return;
            }

            IsUriPathVerified = true;

            // Load the orchestration
            var orchestration = await _loader.LoadFromUrlAsync(UriPath);
            SetOrchestration(orchestration);

            // Add to URL history
            AddToUrlHistory(UriPath);
            AddLog($"Orchestration loaded: {orchestration.Name}");
        }
        catch (Exception ex)
        {
            IsUriPathVerified = false;
            LoadError = ex.Message;
            AppLog.Error("Settings", $"Error loading orchestration from {AppLog.Describe(UriPath)}.", ex);
            AddLog($"Error loading orchestration: {ex.Message}");
        }
        finally
        {
            IsUriLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        IsFileLoading = true;
        IsLocalPathVerified = null;
        Orchestration = null;
        OrchestrationValidationResult = null;
        LoadError = null;
        LocalPath = filePath;
        AddLog($"Loading orchestration from file: {filePath}");
        AppLog.Info("Settings", $"Loading orchestration from {AppLog.Describe(filePath)}.");

        try
        {
            var orchestration = await _loader.LoadFromFileAsync(filePath);
            IsLocalPathVerified = true;
            SetOrchestration(orchestration);
            AddLog($"Orchestration loaded: {orchestration.Name}");
        }
        catch (Exception ex)
        {
            IsLocalPathVerified = false;
            LoadError = ex.Message;
            AppLog.Error("Settings", $"Error loading orchestration from {AppLog.Describe(filePath)}.", ex);
            AddLog($"Error loading file: {ex.Message}");
        }
        finally
        {
            IsFileLoading = false;
        }
    }

    private void SetOrchestration(Orchestration orchestration)
    {
        Orchestration = orchestration;
        var validationResult = orchestration.Validate();
        OrchestrationValidationResult = validationResult;

        AddLog($"Validation: {validationResult.PassedCount} passed, {validationResult.FailedCount} failed");
        AppLog.Info("Settings", $"Orchestration validation: passed={validationResult.PassedCount}, failed={validationResult.FailedCount}.");
        if (!validationResult.IsFullyValid)
            foreach (var failure in GetFailures(validationResult))
            {
                AppLog.Warn("Settings", $"Validation failure: {failure.Message}");
                AddLog($"  ✗ {failure.Identifier}: {failure.Message}");
            }
    }

    private static IEnumerable<ValidationResult> GetFailures(ValidationResult result)
    {
        if (result.IsValid == false)
            yield return result;
        foreach (var child in result.Children)
            foreach (var failure in GetFailures(child))
                yield return failure;
    }

    private void AddToUrlHistory(string url)
    {
        // Remove if already exists (to move it to the top)
        var existing = UrlHistory.FirstOrDefault(u => u.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            UrlHistory.Remove(existing);

        // Add to the front
        UrlHistory.Insert(0, url);

        // Trim to max
        var max = _settings.GetSetting(SettingsKeys.MaxUrlHistory, SettingsKeys.DefaultMaxUrlHistory);
        while (UrlHistory.Count > max)
            UrlHistory.RemoveAt(UrlHistory.Count - 1);

        SaveState();
    }

    /// <summary>
    /// Removes a URL from the history.
    /// </summary>
    public void RemoveFromUrlHistory(string url)
    {
        UrlHistory.Remove(url);
        SaveState();
    }

    public const int MaxLogEntries = 2500;

    public void AddLog(string message)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        LogEntries.Insert(0, entry);
        while (LogEntries.Count > MaxLogEntries)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    /// <summary>
    /// Resets the orchestration and related state.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        Orchestration = null;
        OrchestrationValidationResult = null;
        LoadError = null;
        IsUriPathVerified = null;
        IsLocalPathVerified = null;
        IsAutoRetryActive = false;
        ShouldAutoRetryStart = false;
        CurrentAutoRetryCountdown = 0;
        AddLog("Settings reset.");
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    /// <summary>
    /// Remembers the orchestration that is being run so it can be reloaded on the next startup.
    /// </summary>
    public void RecordLastRun(Orchestration orchestration)
    {
        if (string.IsNullOrWhiteSpace(orchestration.SourcePath)) return;

        AppLog.Debug("Startup", $"Recording last run: source={orchestration.Source}, target={AppLog.Describe(orchestration.SourcePath)}.");
        _settings.SaveSetting(SettingsKeys.LastRunSource, orchestration.Source == OrchestrationSource.File ? "File" : "URL");
        _settings.SaveSetting(SettingsKeys.LastRunPath, orchestration.SourcePath);
    }

    /// <summary>
    /// Attempts to load the last orchestration that was run. Used on application startup to
    /// auto-start the previous session's orchestration.
    /// </summary>
    public async Task<StartupLoadResult> TryAutoStartAsync()
    {
        string? source = null, path = null;
        try
        {
            source = _settings.GetSetting<string>(SettingsKeys.LastRunSource);
            path = _settings.GetSetting<string>(SettingsKeys.LastRunPath);
        }
        catch (Exception ex)
        {
            AppLog.Error("Startup", "Auto-start: could not read last run.", ex);
            return StartupLoadResult.LoadFailed;
        }
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(path))
        {
            AppLog.Info("Startup", "Auto-start: no previous orchestration recorded.");
            return StartupLoadResult.NoPreviousOrchestration;
        }

        AppLog.Info("Startup", $"Auto-start: reloading last orchestration (source={source}, target={AppLog.Describe(path)}).");

        try
        {
            if (source == "File")
            {
                IsLocalFile = true;
                await LoadFromFileCommand.ExecuteAsync(path);
            }
            else
            {
                IsLocalFile = false;
                UriPath = path;
                await LoadFromUriCommand.ExecuteAsync(null);
            }

            return CanStart ? StartupLoadResult.LoadedAndValid : StartupLoadResult.LoadFailed;
        }
        catch (Exception ex)
        {
            AppLog.Error("Startup", "Auto-start: failed to reload the last orchestration.", ex);
            RecoverPaths();
            return StartupLoadResult.LoadFailed;
        }
    }
}
