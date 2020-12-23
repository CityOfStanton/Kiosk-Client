/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Actions.Settings;
using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// A method of displaying some type of supported content.
    /// </summary>
    public class Action
    {
        [JsonIgnore, XmlIgnore]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ActionType Type { get; set; }
        public ActionSettings Settings { get; set; }

        public Action()
        {
            this.Id = Guid.NewGuid();
        }
    }
}
