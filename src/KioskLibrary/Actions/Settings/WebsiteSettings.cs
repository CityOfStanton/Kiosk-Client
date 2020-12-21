/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using System;
using KioskLibrary.Actions;

namespace KioskLibrary.Actions.Settings
{
    /// <summary>
    /// Settings for the <see cref="ActionType.Website"/> <see cref="ActionType"/>.
    /// </summary>
    public record WebsiteSettings(Uri Website)
        : ActionSettings;
}
