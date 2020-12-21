/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using System.Collections.Generic;
using KioskLibrary.Actions;

namespace KioskLibrary.Actions.Settings
{
    /// <summary>
    /// Settings for the <see cref="ActionType.Slideshow"/> <see cref="ActionType"/>.
    /// </summary>
    public record SlideshowSettings(List<SlideshowImageSettings> Images)
        : ActionSettings;
}
