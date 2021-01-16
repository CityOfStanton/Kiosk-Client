/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Actions;
using KioskLibrary.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace KioskLibrary.Orchestration
{
    /// <summary>
    /// An instance of an orchestration
    /// </summary>
    public class OrchestrationInstance
    {
        /// <summary>
        /// The interval used to check for updated versions of this <see cref="OrchestrationInstance" />
        /// </summary>
        public int PollingIntervalMinutes { get; set; }

        /// <summary>
        /// The lifecycle behavior of an <see cref="OrchestrationInstance" />
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LifecycleBehavior Lifecycle { get; set; } = LifecycleBehavior.SingleRun;

        /// <summary>
        /// The order to iterate through the set of <see cref="OrchestrationInstance.Actions" />
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Ordering Order { get; set; } = Ordering.Sequential;

        /// <summary>
        /// The source of this <see cref="OrchestrationInstance" />
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public OrchestrationSource OrchestrationSource { get; set; }

        /// <summary>
        /// A list of <see cref="Action" />s to process
        /// </summary>
        public List<Action> Actions { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OrchestrationInstance() { Actions = new List<Action>(); }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="actions">A list of <see cref="Action" />s to process</param>
        /// <param name="pollingInterval">The interval used to check for updated versions of this <see cref="OrchestrationInstance" /></param>
        /// <param name="orchestrationSource">The source of this <see cref="OrchestrationInstance" /></param>
        /// <param name="lifecycle">The lifecycle behavior of an <see cref="OrchestrationInstance" /></param>
        /// <param name="order">The order to iterate through the set of <see cref="OrchestrationInstance.Actions" /></param>
        public OrchestrationInstance(List<Action> actions, int pollingInterval, OrchestrationSource orchestrationSource, LifecycleBehavior lifecycle = LifecycleBehavior.SingleRun, Ordering order = Ordering.Sequential)
        {
            PollingIntervalMinutes = pollingInterval;
            Actions = actions;
            Lifecycle = lifecycle;
            Order = order;
            OrchestrationSource = orchestrationSource;
        }

        /// <summary>
        /// Validates this <see cref="OrchestrationInstance" />
        /// </summary>
        /// <returns>A boolean indicating whether or not this <see cref="OrchestrationInstance" /> is valid as well as a list of errors (if validation fails)</returns>
        public async Task<(bool, List<string>)> ValidateAsync()
        {
            var errors = new List<string>();

            if (PollingIntervalMinutes < 15)
                errors.Add("OrchestrationInstance: The polling interval cannot be less than 15 minutes.");

            if (Actions != null)
                foreach (var a in Actions)
                {
                    (bool status, string name, List<string> actionErrors) = await a.ValidateAsync();
                    if (!status)
                        foreach (var actionError in actionErrors)
                            errors.Add($"{name}: {actionError}");
                }

            return (!errors.Any(), errors);
        }
    }
}
