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
        private ObservableCollection<ValidationResult> _orchestrationValidation;

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
            _orchestrationValidation = new ObservableCollection<ValidationResult>();
        }

        /// <summary>
        /// The Uri Path
        /// </summary>
        public string UriPath
        {
            get { return _uriPath; }
            set { _uriPath = value; NotifyPropertyChanged(); }
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
            set { _orchestration = value; NotifyPropertyChanged(); }
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
            get { return new TimeSpan(0,0, Orchestration?.Actions?.Sum(x => x.Duration) ?? 0).Humanize(); }
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
    }
}
