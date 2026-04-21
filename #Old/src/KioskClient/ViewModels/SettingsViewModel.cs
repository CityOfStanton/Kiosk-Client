/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Humanizer;
using KioskLibrary.Common;
using KioskLibrary.Orchestrations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace KioskLibrary.ViewModels
{
    /// <summary>
    /// View model for the Settings page
    /// </summary>
    public class SettingsViewModel : ViewModel
    {
        private string _uriPath;
        private string _localPath;
        private bool isLocalFile;
        private bool? _isUriPathVerified;
        private bool? _isLocalPathVerified;
        private string _pathValidationMessage;
        private Orchestration _orchestration;
        private bool _isUriLoading;
        private bool _isFileLoading;
        private ObservableCollection<string> _urlHistory;
        private bool _isAutoRetryEnabled = true;
        private int _autoRetrySeconds = 10;
        private int _currentAutoRetryCountdown;
        private bool _isAutoRetryActive = true;
        private bool _shouldAutoRetryStart = true;

        public delegate void AutoRetryActivationStateChangedEventHandler(bool isActive);
        public event AutoRetryActivationStateChangedEventHandler AutoRetryActivationStateChanged;

        public delegate void SettingsChangedEventHandler();
        public event SettingsChangedEventHandler SettingsChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public SettingsViewModel()
            : base(new List<string>() {
                nameof(OrchestrationValidationResult),
                nameof(IsOrchestrationLoaded),
                nameof(IsOrchestrationValid),
                nameof(CanStart),
                nameof(CanLoadFile),
                nameof(CanLoadUri),
                nameof(OrchestrationSummaryActionCount),
                nameof(OrchestrationSummaryIsValid),
                nameof(OrchestrationSummaryLifecycle),
                nameof(OrchestrationSummaryName),
                nameof(OrchestrationSummaryOrder),
                nameof(OrchestrationSummaryPollingInerval),
                nameof(OrchestrationSummaryRuntime),
                nameof(OrchestrationSummarySource),
                nameof(OrchestrationSummarySourceDisplay),
                nameof(OrchestrationSummaryVersion),
                nameof(IsOrchestrationValidationResultsLoaded),
                nameof(OrchestrationSummaryIsValid),
                nameof(OrchestrationSummaryIsValidDisplay)
            })
        {
            _urlHistory = new ObservableCollection<string>();
            CurrentAutoRetryCountdown = AutoRetrySeconds;
        }

        /// <summary>
        /// Has the state been loaded from storage
        /// </summary>
        [JsonIgnore]
        public bool HasStateBeenLoaded { get; set; }

        /// <summary>
        /// The Uri Path
        /// </summary>
        public string UriPath
        {
            get { return _uriPath; }
            set
            {
                if (_uriPath != value)
                {
                    _uriPath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The path to the local file
        /// </summary>
        public string LocalPath
        {
            get { return _localPath; }
            set { _localPath = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not the we're referencing a local file
        /// </summary>
        public bool IsLocalFile
        {
            get { return isLocalFile; }
            set { isLocalFile = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not the URI path has been verified
        /// </summary>
        public bool? IsUriPathVerified
        {
            get { return _isUriPathVerified; }
            set { _isUriPathVerified = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not the local path has been verified
        /// </summary>
        public bool? IsLocalPathVerified
        {
            get { return _isLocalPathVerified; }
            set { _isLocalPathVerified = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// The message from the last path validation attempt
        /// </summary>
        public string PathValidationMessage
        {
            get { return _pathValidationMessage; }
            set { _pathValidationMessage = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not the Orchestration is valid
        /// </summary>
        public bool IsOrchestrationValid
        {
            get { return Orchestration?.IsValid ?? false; }
        }

        /// <summary>
        /// Whether or not the Orchestration is loaded
        /// </summary>
        public bool IsOrchestrationLoaded
        {
            get { return Orchestration != null; }
        }

        /// <summary>
        /// Whether or not the Orchestration's validation results have been loaded
        /// </summary>
        public bool IsOrchestrationValidationResultsLoaded
        {
            get { return Orchestration?.ValidationResult != null && Orchestration.ValidationResult.Count > 0; }
        }

        /// <summary>
        /// The currently loaded <see cref="Orchestration" />
        /// </summary>
        [JsonIgnore]
        public Orchestration Orchestration
        {
            get { return _orchestration; }
            set
            {
                _orchestration = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsOrchestrationLoaded));
                NotifyPropertyChanged(nameof(IsOrchestrationValidationResultsLoaded));
            }
        }

        /// <summary>
        /// The currently loaded <see cref="Orchestration" />
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ValidationResult> OrchestrationValidationResult
        {
            get { return _orchestration?.ValidationResult; }
        }

        /// <summary>
        /// Indicates that the Uri is in a "loading" state
        /// </summary>
        public bool IsUriLoading
        {
            get { return _isUriLoading; }
            set { _isUriLoading = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Indicates that the local file is in a "loading" state
        /// </summary>
        public bool IsFileLoading
        {
            get { return _isFileLoading; }
            set { _isFileLoading = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not all conditions have been satisfied to run the orchestration
        /// </summary>
        public bool CanStart
        {
            get
            {
                return IsOrchestrationValid
                    &&
                        ((!IsLocalFile && IsUriPathVerified.HasValue && IsUriPathVerified.Value)
                        ||
                        (IsLocalFile && IsLocalPathVerified.HasValue && IsLocalPathVerified.Value));
            }
        }

        /// <summary>
        /// Whether or not all conditions have been satisfied to load a URI
        /// </summary>
        public bool CanLoadUri { get { return !IsUriLoading && !IsLocalFile; } }

        /// <summary>
        /// Whether or not all conditions have been satisfied to load a local file
        /// </summary>
        public bool CanLoadFile { get { return !IsFileLoading && IsLocalFile; } }

        /// <summary>
        /// The OrchestrationSummaryName
        /// </summary>
        public string OrchestrationSummaryName
        {
            get { return Orchestration?.Name ?? ""; }
        }

        /// <summary>
        /// The OrchestrationSummaryOrder
        /// </summary>
        public string OrchestrationSummaryOrder
        {
            get { return Orchestration?.Order.Humanize().Transform(To.TitleCase) ?? ""; }
        }

        /// <summary>
        /// The OrchestrationSummaryLifecycle
        /// </summary>
        public string OrchestrationSummaryLifecycle
        {
            get { return Orchestration?.Lifecycle.Humanize().Transform(To.TitleCase) ?? ""; }
        }

        /// <summary>
        /// The OrchestrationSummaryIsValid
        /// </summary>
        public bool OrchestrationSummaryIsValid
        {
            get { return Orchestration?.IsValid ?? false; }
        }

        /// <summary>
        /// The OrchestrationSummaryIsValidDisplay
        /// </summary>
        public string OrchestrationSummaryIsValidDisplay
        {
            get { return OrchestrationSummaryIsValid ? "Passed" : "Failed"; }
        }

        /// <summary>
        /// The OrchestrationSummaryActionCount
        /// </summary>
        public int OrchestrationSummaryActionCount
        {
            get { return Orchestration?.Actions?.Count ?? 0; }
        }

        /// <summary>
        /// The OrchestrationSummaryRuntime
        /// </summary>
        public string OrchestrationSummaryRuntime
        {
            get { return new TimeSpan(0, 0, Orchestration?.Actions?.Sum(x => x.Duration) ?? 0).Humanize(); }
        }

        /// <summary>
        /// The OrchestrationSummaryVersion
        /// </summary>
        public string OrchestrationSummaryVersion
        {
            get { return Orchestration?.Version ?? ""; }
        }

        /// <summary>
        /// The OrchestrationSummaryPollingInerval
        /// </summary>
        public string OrchestrationSummaryPollingInerval
        {
            get { return new TimeSpan(0, Orchestration?.PollingIntervalMinutes ?? 0, 0).Humanize(); }
        }

        /// <summary>
        /// The OrchestrationSummarySource
        /// </summary>
        public OrchestrationSource OrchestrationSummarySource
        {
            get { return Orchestration?.OrchestrationSource ?? OrchestrationSource.File; }
        }

        /// <summary>
        /// The OrchestrationSummarySourceDisplay
        /// </summary>
        public string OrchestrationSummarySourceDisplay
        {
            get { return Orchestration?.OrchestrationSource.Humanize() ?? OrchestrationSource.File.Humanize(); }
        }

        /// <summary>
        /// Resets an Orchestration
        /// </summary>
        public void Reset()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in properties)
                if (p.CanRead && p.CanWrite)
                    p.SetValue(this, null, null);
        }

        /// <summary>
        /// The URL history
        /// </summary>
        public ObservableCollection<string> UrlHistory
        {
            get { return _urlHistory; }
            set { _urlHistory = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(UriPath)); }
        }

        /// <summary>
        /// Enable or disable auto-retry
        /// </summary>
        public bool IsAutoRetryEnabled
        {
            get => _isAutoRetryEnabled;
            set
            {
                if (!_isAutoRetryEnabled)
                    ShouldAutoRetryStart = IsAutoRetryActive = _isAutoRetryEnabled;

                _isAutoRetryEnabled = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(AutoRetryCountdownDisplay));

                SettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Number of seconds before auto-retry
        /// </summary>
        public int AutoRetrySeconds
        {
            get => _autoRetrySeconds;
            set
            {
                _autoRetrySeconds = value;
                NotifyPropertyChanged();

                SettingsChanged?.Invoke();

                CurrentAutoRetryCountdown = _autoRetrySeconds;
            }
        }

        /// <summary>
        /// Display string for countdown
        /// </summary>
        [JsonIgnore]
        public string AutoRetryCountdownDisplay
        {
            get => IsAutoRetryEnabled ? IsAutoRetryActive ? $"Retrying in {CurrentAutoRetryCountdown} seconds..." : $"Auto-retry stopped with {CurrentAutoRetryCountdown} seconds remainging..." : "Auto-retry disabled";
        }

        /// <summary>
        /// Indicates if auto-retry should be started
        /// </summary>
        [JsonIgnore]
        public bool ShouldAutoRetryStart
        {
            get => _shouldAutoRetryStart;
            set
            {
                _shouldAutoRetryStart = value;
                NotifyPropertyChanged();

                if (IsAutoRetryEnabled)
                    IsAutoRetryActive = _shouldAutoRetryStart;
            }
        }

        /// <summary>
        /// Indicates if auto-retry is currently active (false if disabled)
        /// </summary>
        /// <remarks>To start, set IsAutoRetryEnabled and ShouldAutoRetryStart to true</remarks>
        [JsonIgnore]
        public bool IsAutoRetryActive
        {
            get => _isAutoRetryActive;
            private set
            {
                if (_isAutoRetryActive != value)
                {
                    _isAutoRetryActive = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(AutoRetryCountdownDisplay));
                    AutoRetryActivationStateChanged?.Invoke(_isAutoRetryActive);
                }
            }
        }

        /// <summary>
        /// Current countdown value for auto-retry, resets to AutoRetryCountdown if set below 0
        /// </summary>
        [JsonIgnore]
        public int CurrentAutoRetryCountdown
        {
            get => _currentAutoRetryCountdown;
            set
            {
                if (value < 0)
                    _currentAutoRetryCountdown = AutoRetrySeconds;
                else
                    _currentAutoRetryCountdown = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(AutoRetryCountdownDisplay));
            }
        }
    }
}