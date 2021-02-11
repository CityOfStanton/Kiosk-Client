/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using KioskLibrary.Helpers;
using System.Collections.Generic;
using System.Linq;
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
        /// The number of seconds spent scrolling the webpage
        /// </summary>
        /// <remarks>This number must be greater than or equal to 1.</remarks>
        public int? ScrollingTime { get; set; }

        /// <summary>
        /// The number of seconds after the bottom of the page is reached before resetting the view to the top of the page
        /// </summary>
        /// <remarks>If defined, this number must be greater than or equal to 0.</remarks>
        public int? ScrollingResetDelay { get; set; }

        /// <summary>
        /// The number of seconds to display the Settings button on the Website Action
        /// </summary>
        /// <remarks>This number must be greater than or equal to 1.</remarks>
        public int SettingsDisplayTime { get; set; }

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
        /// <param name="scrollingTime">The number of times a scrolling event happens per second</param>
        /// <param name="scrollingResetDelay">The delay before resetting the scroll to the top</param>
        /// <param name="settingsDisplayTime">The number of seconds to display the Settings button on the Website Action</param>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        public WebsiteAction(string name, int? duration, string path, bool autoScroll, int? scrollingTime, int? scrollingResetDelay, int settingsDisplayTime, IHttpHelper httpHelper = null)
            : base(name, duration)
        {
            Path = path;
            AutoScroll = autoScroll;
            ScrollingTime = scrollingTime;
            ScrollingResetDelay = scrollingResetDelay;
            SettingsDisplayTime = settingsDisplayTime;
            _httpHelper = httpHelper ?? new HttpHelper();
        }

        /// <inheritdoc/>
        public async override Task<(bool IsValid, string Name, List<string> Errors)> ValidateAsync(IHttpHelper httpHelper = null)
        {
            (_, _, var errors) = await base.ValidateAsync(httpHelper);

            (bool isValid, string message) = await (httpHelper ?? _httpHelper).ValidateURI(Path, HttpStatusCode.Ok);

            if (!isValid)
                errors.Add(message);

            if (ScrollingTime.HasValue && ScrollingTime <= 0)
                errors.Add(Constants.ValidationMessages.Actions.WebsiteAction.InvalidScrollingTime);

            if (ScrollingResetDelay.HasValue && ScrollingResetDelay < 0)
                errors.Add(Constants.ValidationMessages.Actions.WebsiteAction.InvalidScrollingResetDelay);

            if (SettingsDisplayTime < 1)
                errors.Add(Constants.ValidationMessages.Actions.WebsiteAction.InvalidSettingsDisplayTime);

            return (!errors.Any(), Name, errors);
        }
    }
}

