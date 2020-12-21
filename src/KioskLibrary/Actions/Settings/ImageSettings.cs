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
    /// Settings for the <see cref="ActionType.Image"/> <see cref="ActionType"/>.
    /// </summary>
    public record ImageSettings(string Path, string Scale)
        : ActionSettings;
}
