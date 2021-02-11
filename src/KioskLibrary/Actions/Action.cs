/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using KioskLibrary.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// A method of displaying some type of supported content.
    /// </summary>
    [XmlInclude(typeof(ImageAction))]
    [XmlInclude(typeof(WebsiteAction))]
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
        /// <remarks>This number must be greater than or equal to 1.</remarks>
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
        /// <param name="httpHelper">The HTTP helper</param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async virtual Task<(bool IsValid, string Name, List<string> Errors)> ValidateAsync(IHttpHelper httpHelper = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var errors = new List<string>();

            if (Duration.HasValue && Duration <= 0)
                errors.Add(Constants.ValidationMessages.Actions.InvalidDuration);

            return (!errors.Any(), null as string, errors);
        }

        public override string ToString() => $"{Id} | {Name}";
    }
}
