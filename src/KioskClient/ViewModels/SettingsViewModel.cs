using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [NotifyPropertyChangedFor(nameof(OrchestrationSummaryPollingInterval))]
    [NotifyPropertyChangedFor(nameof(OrchestrationUsesDeprecatedPollingInterval))]
    [NotifyPropertyChangedFor(nameof(CanSaveWithUpdatedFormat))]
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
    private int _autoRetrySeconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoRetryCountdownDisplay))]
    private int _currentAutoRetryCountdown;

    [ObservableProperty]
    private bool _isAutoRetryActive;

    [ObservableProperty]
    private bool _shouldAutoRetryStart;

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
    public string OrchestrationSummaryPollingInterval => Orchestration is not null
        ? $"{Orchestration.PollingInterval} seconds"
        : "N/A";
    public bool OrchestrationUsesDeprecatedPollingInterval => Orchestration?.UsedDeprecatedPollingIntervalMinutes == true;
    public bool CanSaveWithUpdatedFormat => OrchestrationUsesDeprecatedPollingInterval && Orchestration?.Source == OrchestrationSource.File;
    public string AutoRetryCountdownDisplay => $"{CurrentAutoRetryCountdown}s";

    public SettingsViewModel(ISettingsService settings, IOrchestrationLoader loader, IHttpService httpService)
    {
        _settings = settings;
        _loader = loader;
        _httpService = httpService;
        LoadState();
    }

    /// <summary>
    /// Loads saved state from application storage.
    /// </summary>
    private void LoadState()
    {
        IsAutoRetryEnabled = _settings.GetSetting(SettingsKeys.AutoRetryEnabled, true);
        AutoRetrySeconds = _settings.GetSetting(SettingsKeys.RetryTimeoutSeconds, SettingsKeys.DefaultRetryTimeoutSeconds);

        var maxHistory = _settings.GetSetting(SettingsKeys.MaxUrlHistory, SettingsKeys.DefaultMaxUrlHistory);
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

        try
        {
            // Validate URI first
            var validation = await _httpService.ValidateUriAsync(UriPath);
            if (validation.IsValid != true)
            {
                IsUriPathVerified = false;
                AddLog($"URI validation failed: {validation.Message}");
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
        if (!validationResult.IsFullyValid)
            foreach (var failure in GetFailures(validationResult))
                AddLog($"  ✗ {failure.Identifier}: {failure.Message}");
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

    public void AddLog(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogEntries.Insert(0, entry);
    }

    /// <summary>
    /// Resets the deprecated-field flag after the orchestration has been saved with the
    /// updated format, and re-evaluates the related computed properties.
    /// </summary>
    public void NotifyOrchestrationSaved()
    {
        if (Orchestration is not null)
            Orchestration.UsedDeprecatedPollingIntervalMinutes = false;
        OnPropertyChanged(nameof(OrchestrationUsesDeprecatedPollingInterval));
        OnPropertyChanged(nameof(CanSaveWithUpdatedFormat));
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
    /// Attempts to load the last saved orchestration. Used on application startup to
    /// auto-start the previous session's orchestration.
    /// </summary>
    public async Task<StartupLoadResult> TryAutoStartAsync()
    {
        var source = _settings.GetSetting<string>(SettingsKeys.OrchestrationSource);
        if (string.IsNullOrEmpty(source))
            return StartupLoadResult.NoPreviousOrchestration;

        try
        {
            if (source == "File")
            {
                var filePath = _settings.GetSetting<string>(SettingsKeys.OrchestrationFilePath);
                if (string.IsNullOrEmpty(filePath))
                    return StartupLoadResult.NoPreviousOrchestration;

                await LoadFromFileCommand.ExecuteAsync(filePath);
            }
            else
            {
                var uri = _settings.GetSetting<string>(SettingsKeys.OrchestrationUri);
                if (string.IsNullOrEmpty(uri))
                    return StartupLoadResult.NoPreviousOrchestration;

                await LoadFromUriCommand.ExecuteAsync(null);
            }

            return CanStart ? StartupLoadResult.LoadedAndValid : StartupLoadResult.LoadFailed;
        }
        catch
        {
            return StartupLoadResult.LoadFailed;
        }
    }
}
