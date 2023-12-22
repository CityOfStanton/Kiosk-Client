/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using KioskLibrary.Helpers;
using System;
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
        /// The version of this action
        /// </summary>
        public string Version { get; set; } = Constants.Actions.CurrentVersion;

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
        public async virtual Task<ValidationResult> ValidateAsync(IHttpHelper httpHelper = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var result = new ValidationResult(Name);

            if (Duration.HasValue)
                if (Duration <= 0)
                    result.Children.Add(new ValidationResult(nameof(Duration), false, Constants.Validation.Actions.InvalidDuration, Constants.Validation.Actions.DurationGuidance));
                else
                    result.Children.Add(new ValidationResult(nameof(Duration), true, Constants.Validation.Actions.Valid, Constants.Validation.Actions.DurationGuidance));
            else
                result.Children.Add(new ValidationResult(nameof(Duration), true, Constants.Validation.Actions.NotSet, Constants.Validation.Actions.DurationGuidance));

            return result;
        }

        public override string ToString() => $"{Id} | {Name}";
    }
}
