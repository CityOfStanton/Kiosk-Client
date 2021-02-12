/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

namespace KioskLibrary.Common
{
    /// <summary>
    /// Constant values that are reused throughout the application
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Warning, errors, and notifications relating to values supplied for various objects
        /// </summary>
        public class ValidationMessages
        {
            /// <summary>
            /// Warning, errors, and notifications relating to values supplied for <see cref="Orchestration.OrchestrationInstance"/>s
            /// </summary>
            public class OrchestrationInstance
            {
                /// <summary>
                /// Message that indicates a URL has passed validation
                /// </summary>
                public static string ValidURIMessage = "URL is valid!";
                /// <summary>
                /// Error that indicates the polling interval cannot be less than 15 minutes according to <see cref="https://docs.microsoft.com/en-us/windows/uwp/launch-resume/run-a-background-task-on-a-timer-#create-a-time-trigger"/>
                /// </summary>
                public static string InvalidPollingInterval = $"{nameof(Orchestration.OrchestrationInstance)}: {nameof(Orchestration.OrchestrationInstance.PollingIntervalMinutes)} cannot be less than 15 minutes.";
            }

            /// <summary>
            /// Warning and errors with the values supplied for <see cref="KioskLibrary.Actions.Action"/>s
            /// </summary>
            public class Actions
            {
                /// <summary>
                /// Error that indicates the duration for an Action must be greater than 0
                /// </summary>
                public static string InvalidDuration = $"{nameof(KioskLibrary.Actions.Action.Duration)} must be greater than or equal to 1.";

                public class WebsiteAction
                {
                    /// <summary>
                    /// Error that indicates the <see cref="KioskLibrary.Actions.WebsiteAction.ScrollingTime"/> is less that 1
                    /// </summary>
                    public static string InvalidScrollingTime = $"{nameof(KioskLibrary.Actions.WebsiteAction.ScrollingTime)} must be greater than or equal to 1.";
                    /// <summary>
                    /// Error that indicates the <see cref="KioskLibrary.Actions.WebsiteAction.SettingsDisplayTime"/> is less that 1
                    /// </summary>
                    public static string InvalidSettingsDisplayTime = $"{nameof(KioskLibrary.Actions.WebsiteAction.SettingsDisplayTime)} must be greater than or equal to 1.";
                    /// <summary>
                    /// Error that indicates the <see cref="KioskLibrary.Actions.WebsiteAction.ScrollingResetDelay"/> is less that 0
                    /// </summary>
                    public static string InvalidScrollingResetDelay = $"{nameof(KioskLibrary.Actions.WebsiteAction.ScrollingResetDelay)} must be greater than or equal to 0.";
                }
            }
        }

        /// <summary>
        /// Keys used for storing objects in local application storage
        /// </summary>
        public class ApplicationStorage
        {
            /// <summary>
            /// The default <see cref="Orchestration.OrchestrationInstance" />
            /// </summary>
            public static string DefaultOrchestration = "#DefaultOrchestration";
            /// <summary>
            /// The <see cref="OrchestrationSource" /> of the default <see cref="Orchestration.OrchestrationInstance" />
            /// </summary>
            public static string DefaultOrchestrationSource = "#DefaultOrchestrationSource";
            /// <summary>
            /// The URI of the default <see cref="Orchestration.OrchestrationInstance" />
            /// </summary>
            public static string DefaultOrchestrationURI = "#DefaultOrchestrationURI";
            /// <summary>
            /// The next <see cref="Orchestration.OrchestrationInstance" /> that was pulled in from the OrchestrationPollingManager
            /// </summary>
            public static string NextOrchestration = "#NextOrchestration";
            /// <summary>
            /// The ViewModel for the Settings page
            /// </summary>
            public static string SettingsViewModel = "#SettingsViewModel";
            /// <summary>
            ///The polling interval for the OrchestrationPollingManager
            /// </summary>
            public static string PollingInterval = "#PollingInterval";
            /// <summary>
            /// Indicated wether we should end currently running Orchestration
            /// </summary>
            public static string EndOrchestration = "#EndOrchestration";
            /// <summary>
            /// Whether or not to show the Tutorial prompt on startup
            /// </summary>
            public static string DoNotShowTutorialOnStartup = "#DoNotShowTutorialOnStartup";
        }

        /// <summary>
        /// Messages used by the <see cref="Orchestrator"/>
        /// </summary>
        public class Orchestrator
        {
            public static string NoValidOrchestration = "No valid orchestration was loaded. Going to Settings.";
        }

        /// <summary>
        /// Messages used by the Kiosk Client appplication
        /// </summary>
        public class Application
        {
            /// <summary>
            /// Error messages thrown by the application
            /// </summary>
            public class Exceptions
            {
                /// <summary>
                /// Partial error message that occurs when there is no Page mapped to an <see cref="Actions.Action"/>
                /// </summary>
                public static string PageDoesNotExist = "There is no corresponding Page mapped to ";
            }

            /// <summary>
            /// Message returned from the <see cref="PageHelper"/>
            /// </summary>
            public class PageHelper
            {
                /// <summary>
                /// Message that occurs when the Orchestration is cancelled.
                /// </summary>
                public static string OrchestrationCancelled = "Orchestration was cancelled.";
            }

            /// <summary>
            /// Values supplied to the Example Orchestration file
            /// </summary>
            public class OrchestrationFileExample
            {
                /// <summary>
                /// The name of the <see cref="Orchestration.OrchestrationInstance"/>
                /// </summary>
                public static string Name = "Example demo project for ";

                /// <summary>
                /// Values supplied to the <see cref="Actions.ImageAction"/>
                /// </summary>
                public class ImageActionExample
                {
                    /// <summary>
                    /// The name of the <see cref="Actions.ImageAction"/>
                    /// </summary>
                    public static string Name = "Show the Kiosk Client Social Share image from GitHub";
                    /// <summary>
                    /// The path of the <see cref="Actions.ImageAction"/>
                    /// </summary>
                    public static string Path = "https://raw.githubusercontent.com/CityOfStanton/Kiosk-Client/main/logo/Kiosk-Client_GitHub%20Social%20Preview.png";
                }

                /// <summary>
                /// Values supplied to the <see cref="Actions.WebsiteAction"/>
                /// </summary>
                public class WebsiteExample
                {
                    /// <summary>
                    /// The name of the <see cref="Actions.WebsiteAction"/>
                    /// </summary>
                    public static string Name = "Display the Kiosk Client GitHub page";
                    /// <summary>
                    /// The path of the <see cref="Actions.WebsiteAction"/>
                    /// </summary>
                    public static string Path = "https://github.com/CityOfStanton/Kiosk-Client";
                }
            }
        }
    }
}
