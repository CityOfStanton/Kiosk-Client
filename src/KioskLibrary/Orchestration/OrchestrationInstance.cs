/*
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using KioskLibrary.Actions;
using KioskLibrary.Common;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KioskLibrary.Orchestration
{
    public class OrchestrationInstance
    {
        public List<Action> Actions { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LifecycleBehavior Lifecycle { get; set; } = LifecycleBehavior.SingleRun;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Ordering Order { get; set; } = Ordering.Sequential;

        public OrchestrationInstance() { Actions = new List<Action>(); }

        public OrchestrationInstance(List<Action> actions, LifecycleBehavior lifecycle = LifecycleBehavior.SingleRun, Ordering order = Ordering.Sequential)
        {
            Actions = actions;
            Lifecycle = lifecycle;
            Order = order;
        }
    }
}
