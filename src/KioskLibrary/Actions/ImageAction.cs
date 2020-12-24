/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

namespace KioskLibrary.Actions
{
    /// <summary>
    /// Settings for displaying a single image
    /// </summary>
    public class ImageAction : Action
    {
        public string Path { get; set; }
        public Windows.UI.Xaml.Media.Stretch Stretch { get; set; }

        public ImageAction() { }

        public ImageAction(string name, int? duration, string path, Windows.UI.Xaml.Media.Stretch stretch)
            : base(name, duration)
        {
            Path = path;
            Stretch = stretch;
        }
    }
}