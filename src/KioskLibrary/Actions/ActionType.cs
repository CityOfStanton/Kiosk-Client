/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

namespace KioskLibrary.Actions
{
    /// <summary>
    /// The types of <see cref="Action"/>s that are supported.
    /// </summary>
    /// <remarks>
    /// Must match to a corresponding <see cref="Settings.ActionSettings"/>.
    /// </remarks>
    public enum ActionType
    {
        Image,
        Slideshow,
        Website
    }
}
