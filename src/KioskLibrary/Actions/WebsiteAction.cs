/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

namespace KioskLibrary.Actions
{
    /// <summary>
    /// Settings for displaying a webpage.
    /// </summary>
    public class WebsiteAction
        : Action
    {
        public string Path { get; set; }

        public bool AutoScroll { get; set; }

        public int? ScrollDuration { get; set; }

        public double? ScrollInterval { get; set; }

        public int? ScrollResetDelay { get; set; }

        public WebsiteAction() { }

        public WebsiteAction(string name, int? duration, string path, bool autoScroll, int? scrollDuration, double? scrollInterval, int? scrollResetDelay)
            : base(name, duration)
        {
            Path = path;
            AutoScroll = autoScroll;
            ScrollDuration = scrollDuration;
            ScrollInterval = scrollInterval;
            ScrollResetDelay = scrollResetDelay;
        }
    }
}

