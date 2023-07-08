/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using KioskLibrary.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// Settings for displaying a single image
    /// </summary>
    [Cacheable]
    public class ImageAction : Action, IActionPath
    {
        /// <summary>
        /// The path to the image
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The type of <see cref="Stretch"/> to apply to the image
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public Stretch Stretch { get; set; }

        private readonly IHttpHelper _httpHelper;

        /// <summary>
        /// Constructor
        /// </summary>
        public ImageAction() { _httpHelper = new HttpHelper(); }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the action</param>
        /// <param name="duration">The duration of the action</param>
        /// <param name="path">The path to the iamge</param>
        /// <param name="stretch">The <see cref="Stretch" /> to apply to the image</param>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        public ImageAction(string name, int? duration, string path, Stretch stretch, IHttpHelper httpHelper = null)
            : base(name, duration)
        {
            Path = path;
            Stretch = stretch;
            _httpHelper = httpHelper ?? new HttpHelper();
        }

        /// <inheritdoc/>
        public async override Task<ValidationResult> ValidateAsync(IHttpHelper httpHelper = null)
        {
            var result = await base.ValidateAsync(httpHelper);

            var pathResult = await (httpHelper ?? _httpHelper).ValidateURI(Path, HttpStatusCode.Ok, nameof(Path));

            result.Children.Add(pathResult);

            return result;
        }
    }
}