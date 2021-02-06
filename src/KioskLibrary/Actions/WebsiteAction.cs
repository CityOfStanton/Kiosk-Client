/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
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
        /// <summary>
        /// The path to the website
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether or not to auto scroll the page
        /// </summary>
        public bool AutoScroll { get; set; }

        /// <summary>
        /// How long to scroll the page
        /// </summary>
        /// <remarks>The longer the duration, the slower the scrolling effect</remarks>
        public int? ScrollDuration { get; set; }

        /// <summary>
        /// The delay between scrolling
        /// </summary>
        /// <remarks>The smaller the interval, the smoother the scroll will appear</remarks>
        public double? ScrollInterval { get; set; }

        /// <summary>
        /// The delay before resetting the scroll to the top
        /// </summary>
        public int? ScrollResetDelay { get; set; }

        private readonly IHttpHelper _httpHelper;

        /// <summary>
        /// Constructor
        /// </summary>
        public WebsiteAction() { _httpHelper = new HttpHelper(); }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the action</param>
        /// <param name="duration">The duration of the action</param>
        /// <param name="path">The path to the website</param>
        /// <param name="autoScroll">Whether or not to auto scroll the page</param>
        /// <param name="scrollDuration">How long to scroll the page</param>
        /// <param name="scrollInterval">The delay between scrolling</param>
        /// <param name="scrollResetDelay">The delay before resetting the scroll to the top</param>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        public WebsiteAction(string name, int? duration, string path, bool autoScroll, int? scrollDuration, double? scrollInterval, int? scrollResetDelay, IHttpHelper httpHelper = null)
            : base(name, duration)
        {
            Path = path;
            AutoScroll = autoScroll;
            ScrollDuration = scrollDuration;
            ScrollInterval = scrollInterval;
            ScrollResetDelay = scrollResetDelay;
            _httpHelper = httpHelper ?? new HttpHelper();
        }

        /// <inheritdoc/>
        public async override Task<(bool IsValid, string Name, List<string> Errors)> ValidateAsync(IHttpHelper httpHelper = null)
        {
            (bool isValid, string message) = await (httpHelper ?? _httpHelper).ValidateURI(Path, HttpStatusCode.Ok);

            var errors = new List<string>();
            if (!isValid)
                errors.Add(message);

            return (isValid, Name, errors);
        }
    }
}

