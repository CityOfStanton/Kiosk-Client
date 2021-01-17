/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static KioskLibrary.Converters.StringExtension;

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
        /// <summary>
        /// Ths id of the action
        /// </summary>
        /// <value></value>
        [JsonIgnore, XmlIgnore]
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the action
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The duration of the action
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Action()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the action</param>
        /// <param name="duration">The duration of the action</param>
        public Action(string name, int? duration)
        {
            Name = name;
            Duration = duration;
        }

        /// <summary>
        /// Validates the action
        /// </summary>
        /// <param name="The name of the action"></param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async virtual Task<(bool, string, List<string>)> ValidateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException();
        }
    }
}
