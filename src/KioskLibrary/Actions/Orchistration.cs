/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using System.Collections.Generic;

namespace KioskLibrary.Actions
{
    public class Orchistration
    {
        public List<Action> Actions { get; set; }
        public LifecycleBehavior Lifecycle { get; set; } = LifecycleBehavior.ContnuousLoop;

        public Orchistration() { Actions = new List<Action>(); }

        public Orchistration(List<Action> actions, LifecycleBehavior lifecycle = LifecycleBehavior.ContnuousLoop)
        {
            Actions = actions;
            Lifecycle = lifecycle;
        }
    }
}
