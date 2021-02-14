/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary;
using KioskLibrary.Common;
using KioskLibrary.Helpers;
using KioskLibrary.Storage;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;

namespace OrchestrationPollingManager
{
    /// <summary>
    /// Background update task that polls the orchestration URL in order to keep it up-to-date
    /// </summary>
    public sealed class OrchestrationUpdateTask : IBackgroundTask
    {
        private static readonly string _taskName = "OrchestrationInstanceUpdateTask";

        /// <summary>
        /// The method called by the background worker framework
        /// </summary>
        /// <param name="taskInstance">The background task instance</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            Log.Information("OrchestrationUpdateTask Run invoked");

            await Orchestrator.GetNextOrchestration(new HttpHelper(), new ApplicationStorage());

            deferral.Complete();
        }

        /// <summary>
        /// Registers the Orchestration Instance updater
        /// </summary>
        public static IAsyncOperation<bool> RegisterOrchestrationInstanceUpdater()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks.Where(x => x.Value.Name.Contains(_taskName)))
                task.Value.Unregister(true);

            return RegisterOrchestrationInstanceUpdaterHelper().AsAsyncOperation();
        }

        private async static Task<bool> RegisterOrchestrationInstanceUpdaterHelper()
        {
            Log.Information("RegisterOrchestrationInstanceUpdaterHelper invoked");

            var pollingInterval = new ApplicationStorage().GetSettingFromStorage<int>(Constants.ApplicationStorage.Settings.PollingInterval);

            if (pollingInterval > 0)
            {
                await BackgroundExecutionManager.RequestAccessAsync();
                var btb = new BackgroundTaskBuilder
                {
                    Name = _taskName,
                    TaskEntryPoint = typeof(OrchestrationUpdateTask).FullName
                };
                var tt = new TimeTrigger(Convert.ToUInt32(pollingInterval), false);
                btb.SetTrigger(tt);
                btb.Register();
                return true;
            }

            return false;
        }
    }
}
