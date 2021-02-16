/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.Collections.Generic;

namespace KioskLibrary.ViewModels
{
    /// <summary>
    /// View model for an Action page
    /// </summary>
    public class ActionViewModel : ViewModel
    {
        private bool? _isContentSourceValid;
        private string _failedToLoadContentMessageDetail;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActionViewModel()
            : base(new List<string>() { nameof(ShowLoadingMessage), nameof(ShowFailedToLoadContentMessage), nameof(ShowContentControl) }) { }

        /// <summary>
        /// Is the content source valid?
        /// </summary>
        public bool? IsContentSourceValid
        {
            get { return _isContentSourceValid; }
            set { _isContentSourceValid = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// The "failed to load content" message detail
        /// </summary>
        public string FailedToLoadContentMessageDetail
        {
            get { return _failedToLoadContentMessageDetail; }
            set { _failedToLoadContentMessageDetail = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not to show the loading message
        /// </summary>
        public bool ShowLoadingMessage
        {
            get { return !_isContentSourceValid.HasValue; }
        }

        /// <summary>
        /// Whether or not to show the "failed to load" message
        /// </summary>
        public bool ShowFailedToLoadContentMessage
        {
            get { return _isContentSourceValid.HasValue && !_isContentSourceValid.Value; }
        }

        /// <summary>
        /// Whether or not to show the content control
        /// </summary>
        public bool ShowContentControl
        {
            get { return _isContentSourceValid.HasValue && _isContentSourceValid.Value; }
        }
    }
}
