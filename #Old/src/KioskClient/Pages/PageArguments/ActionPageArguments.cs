/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

namespace KioskClient.Pages.PageArguments
{
    /// <summary>
    /// Arguments for an Action page
    /// </summary>
    public class ActionPageArguments
    {
        /// <summary>
        /// The action to execute
        /// </summary>
        public KioskLibrary.Actions.Action Action { get; set; }

        /// <summary>
        /// The method that can be called to cancel the orchestration
        /// </summary>
        public System.Action CancelOrchestration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancelOrchestration"></param>
        public ActionPageArguments(KioskLibrary.Actions.Action action, System.Action cancelOrchestration)
        {
            Action = action;
            CancelOrchestration = cancelOrchestration;
        }
    }
}
