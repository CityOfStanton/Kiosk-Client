using KioskLibrary.Actions.Orchestration;
using KioskLibrary.Orchestration;
using System.Collections.Generic;

namespace KioskClient.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        private string _uriPath;
        public string UriPath
        {
            get { return _uriPath; }
            set { _uriPath = value; NotifyPropertyChanged(); }
        }

        private string _localPath;
        public string LocalPath
        {
            get { return _localPath; }
            set { _localPath = value; NotifyPropertyChanged(); }
        }

        private bool isLocalFile;
        public bool IsLocalFile
        {
            get { return isLocalFile; }
            set { isLocalFile = value; NotifyPropertyChanged(); }
        }

        private bool? _isUriPathVerified;
        public bool? IsUriPathVerified
        {
            get { return _isUriPathVerified; }
            set { _isUriPathVerified = value; NotifyPropertyChanged(); }
        }

        private bool? _isLocalPathVerified;
        public bool? IsLocalPathVerified
        {
            get { return _isLocalPathVerified; }
            set { _isLocalPathVerified = value; NotifyPropertyChanged(); }
        }

        private string _pathValidationMessage;
        public string PathValidationMessage
        {
            get { return _pathValidationMessage; }
            set { _pathValidationMessage = value; NotifyPropertyChanged(); }
        }

        public bool DoesOrchestrationHaveContent
        {
            get { return Orchestration != null ; }
        }

        private Orchestration _orchestration;
        public Orchestration Orchestration
        {
            get { return _orchestration; }
            set { _orchestration = value; NotifyPropertyChanged(); }
        }

        private int _uriPollingInterval;
        public int UriPollingInterval
        {
            get { return _uriPollingInterval; }
            set { _uriPollingInterval = value; NotifyPropertyChanged(); }
        }

        private PollingInterval _pollingInterval;
        public PollingInterval PollingInterval
        {
            get { return _pollingInterval; }
            set { _pollingInterval = value; NotifyPropertyChanged(); }
        }

        public bool CanStart
        {
            get
            {
                return DoesOrchestrationHaveContent
                    && 
                        ((PollingInterval != null && IsUriPathVerified.HasValue && IsUriPathVerified.Value) 
                        || 
                        (IsLocalPathVerified.HasValue && IsLocalPathVerified.Value));
            }
        }

        public void ClearVerificationStates()
        {
            IsUriPathVerified = null;
            IsLocalPathVerified = null;
        }

        public SettingsViewModel()
            : base(new List<string>() { nameof(CanStart) })
        {
            
        }
    }
}
