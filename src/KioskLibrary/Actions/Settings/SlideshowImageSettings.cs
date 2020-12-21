/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Actions;

namespace KioskLibrary.Actions.Settings
{
    /// <summary>
    /// Settings for particular images in the <see cref="ActionType.Slideshow"/> <see cref="ActionType"/>.
    /// </summary>
    public record SlideshowImageSettings(string Path, string Scale, int DisplayTime)
        : ImageSettings(Path, Scale);
}
