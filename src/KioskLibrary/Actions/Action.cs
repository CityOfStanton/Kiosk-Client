/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Actions.Settings;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// A method of displaying some type of supported content.
    /// </summary>
    public record Action(string Name, ActionType Type, ActionSettings Settings);
}
