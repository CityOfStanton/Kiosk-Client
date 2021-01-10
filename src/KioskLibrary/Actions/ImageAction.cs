/*  
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Helpers;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Stretch Stretch { get; set; }

        public ImageAction() { }

        public ImageAction(string name, int? durationSeconds, string path, Windows.UI.Xaml.Media.Stretch stretch)
            : base(name, durationSeconds)
        {
            Path = path;
            Stretch = stretch;
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