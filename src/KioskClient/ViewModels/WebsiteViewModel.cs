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
    /// View model for the WebsiteAction page
    /// </summary>
    public class WebsiteViewModel : ViewModel
    {
        private bool _isLoaded;
        private bool _isWebpageValid;

        /// <summary>
        /// Constructor
        /// </summary>
        public WebsiteViewModel()
            : base(new List<string>() { nameof(ShowLoadingMessage), nameof(ShowWebpage) }) { }

        /// <summary>
        /// Is the Website loaded?
        /// </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { _isLoaded = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Is the web page valid?
        /// </summary>
        public bool IsWebpageValid
        {
            get { return _isWebpageValid; }
            set { _isWebpageValid = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Whether or not to show the loading message
        /// </summary>
        public bool ShowLoadingMessage
        {
            get { return !_isLoaded; }
        }

        /// <summary>
        /// Whether or not to show the webpage
        /// </summary>
        public bool ShowWebpage
        {
            get { return _isLoaded && _isWebpageValid; }
        }
    }
}
