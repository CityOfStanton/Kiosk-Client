/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async virtual Task<(bool, string, List<string>)> ValidateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException();
        }
    }
}
