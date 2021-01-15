/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary;
using KioskLibrary.Common;
using KioskLibrary.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;

namespace OrchestrationPollingManager
{
    public sealed class OrchestrationUpdateTask : IBackgroundTask
    {
        private static string _taskName = "OrchestrationInstanceUpdateTask";

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            await Orchestrator.GetNextOrchestration();

            deferral.Complete();
        }

        public static IAsyncOperation<bool> RegisterOrchestrationInstanceUpdater()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks.Where(x => x.Value.Name.Contains(_taskName)))
                task.Value.Unregister(true);

            return RegisterOrchestrationInstanceUpdaterHelper().AsAsyncOperation();
        }

        private async static Task<bool> RegisterOrchestrationInstanceUpdaterHelper()
        {
            var pollingInterval = ApplicationStorage.GetFromStorage<int>(Constants.PollingInterval);

            if (pollingInterval > 0)
            {
                await BackgroundExecutionManager.RequestAccessAsync();
                var btb = new BackgroundTaskBuilder();
                btb.Name = _taskName;
                btb.TaskEntryPoint = typeof(OrchestrationUpdateTask).FullName;
                var tt = new TimeTrigger(Convert.ToUInt32(pollingInterval), false);
                btb.SetTrigger(tt);
                btb.Register();
                return true;
            }

            return false;
        }
    }
}
