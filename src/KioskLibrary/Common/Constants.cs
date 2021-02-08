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
        public class ValidationMessages
        {
            public static string ValidURIMessage = "URL is valid!";
            public static string InvalidPollingMessage = "OrchestrationInstance: The polling interval cannot be less than 15 minutes.";

            public class ActionValidationErrors
            {
                public static string Duration = "Duration must be greater than 0.";
            }

            public class WebsiteActionValidationErrors
            {
                public static string ScrollDuration = "ScrollDuration must be greater than or equalt to 0.";
                public static string SettingsDisplayTime = "SettingsDisplayTime must be greater than or equalt to 0.";
                public static string ScrollInterval = "ScrollInterval must be greater than or equalt to 0.";
                public static string ScrollResetDelay = "ScrollResetDelay must be greater than or equalt to 0.";
            }
        }

        /// <summary>
        /// Keys used for storing objects in local application storage
        /// </summary>
        public class ApplicationStorage
        {
            public static string CurrentOrchestration = "#CurrentOrchestration";
            public static string CurrentOrchestrationSource = "#CurrentOrchestrationSource";
            public static string CurrentOrchestrationURI = "#CurrentOrchestrationURI";
            public static string NextOrchestration = "#NextOrchestration";
            public static string SettingsViewModel = "#SettingsViewModel";
            public static string PollingInterval = "#PollingInterval";
            public static string EndOrchestration = "#EndOrchestration";
        }
    }
}
