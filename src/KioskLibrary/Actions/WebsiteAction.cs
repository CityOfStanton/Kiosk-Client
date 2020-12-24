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
        public string Path{ get; set; }

        public WebsiteAction() { }

        public WebsiteAction(string name, int? duration, string path)
            : base(name, duration)
        {
            Path = path;
        }
    }
}

