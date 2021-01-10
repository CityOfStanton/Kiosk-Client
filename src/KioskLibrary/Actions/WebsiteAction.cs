/*  
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Web.Http;

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

        public async override Task<(bool, string, List<string>)> ValidateAsync()
        {
            (bool isValid, string message) = await HttpHelper.ValidateURI(Path, HttpStatusCode.Ok);

            var errors = new List<string>();
            if (!isValid)
                errors.Add(message);

            return (isValid, Name, errors);
        }
    }
}

