/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// Settings for displaying a single image
    /// </summary>
    public class ImageAction : Action
    {
        public string Path { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Stretch Stretch { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ImageAction() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the action</param>
        /// <param name="durationSeconds">The duration of the action</param>
        /// <param name="path">The path to the iamge</param>
        /// <param name="stretch">The <see cref="Stretch" /> to apply to the image</param>
        public ImageAction(string name, int? durationSeconds, string path, Stretch stretch)
            : base(name, durationSeconds)
        {
            Path = path;
            Stretch = stretch;
        }

        /// <summary>
        /// Validates the action
        /// </summary>
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