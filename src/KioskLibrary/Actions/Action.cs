/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Converters;
using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// A method of displaying some type of supported content.
    /// </summary>
    [XmlInclude(typeof(ImageAction))]
    [XmlInclude(typeof(WebsiteAction))]
    [JsonConverter(typeof(ActionConverter))]
    public abstract class Action
    {
        [JsonIgnore, XmlIgnore]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int? Duration { get; set; }

        public Action()
        {
            Id = Guid.NewGuid();
        }

        public Action(string name, int? duration)
        {
            Name = name;
            Duration = duration;
        }
    }
}
