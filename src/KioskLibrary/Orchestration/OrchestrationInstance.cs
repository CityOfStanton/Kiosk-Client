/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
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
    public class OrchestrationInstance
    {
        public int PollingIntervalMinutes { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LifecycleBehavior Lifecycle { get; set; } = LifecycleBehavior.SingleRun;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Ordering Order { get; set; } = Ordering.Sequential;
        [JsonIgnore, XmlIgnore]
        public OrchestrationSource OrchestrationSource { get; set; }
        public List<Action> Actions { get; set; }

        public OrchestrationInstance() { Actions = new List<Action>(); }

        public OrchestrationInstance(List<Action> actions, int pollingInterval, OrchestrationSource orchestrationSource, LifecycleBehavior lifecycle = LifecycleBehavior.SingleRun, Ordering order = Ordering.Sequential)
        {
            PollingIntervalMinutes = pollingInterval;
            Actions = actions;
            Lifecycle = lifecycle;
            Order = order;
            OrchestrationSource = orchestrationSource;
        }

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
