/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Orchestration;
using System.Collections.Generic;

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
        private OrchestrationInstance _orchestrationInstance;
        private bool _isUriLoading;
        private bool _isFileLoading;

        /// <summary>
        /// Constructor
        /// </summary>
        public SettingsViewModel()
            : base(new List<string>() { nameof(CanStart), nameof(CanLoadFile), nameof(CanLoadUri) }) { }

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
        /// Whether or not the Orchestration
        /// </summary>
        public bool DoesOrchestrationInstanceHaveContent
        {
            get { return OrchestrationInstance != null; }
        }

        /// <summary>
        /// The currently loaded <see cref="OrchestrationInstance" />
        /// </summary>
        public OrchestrationInstance OrchestrationInstance
        {
            get { return _orchestrationInstance; }
            set { _orchestrationInstance = value; NotifyPropertyChanged(); }
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
                return DoesOrchestrationInstanceHaveContent
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
    }
}
