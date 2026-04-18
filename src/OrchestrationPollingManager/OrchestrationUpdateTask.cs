/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using KioskLibrary.Helpers;
using KioskLibrary.Orchestrations;
using KioskLibrary.Storage;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrchestrationPollingManager
{
    /// <summary>
    /// Background update task that polls the orchestration URL in order to keep it up-to-date.
    /// Uses an in-app timer instead of UWP IBackgroundTask (not available in Windows App SDK).
    /// </summary>
    public sealed class OrchestrationUpdateTask : IDisposable
    {
        private static Timer _pollingTimer;
        private static readonly object _lock = new object();
        private static bool _isDisposed;

        /// <summary>
        /// Registers the Orchestration updater using an in-app timer
        /// </summary>
        public static Task<bool> RegisterOrchestrationUpdater()
        {
            return RegisterOrchestrationUpdaterHelper();
        }

        /// <summary>
        /// Unregisters the Orchestration updater
        /// </summary>
        public static void UnregisterOrchestrationUpdater()
        {
            lock (_lock)
            {
                _pollingTimer?.Dispose();
                _pollingTimer = null;
            }
        }

        private static Task<bool> RegisterOrchestrationUpdaterHelper()
        {
            Log.Information("RegisterOrchestrationUpdaterHelper invoked");

            var pollingInterval = new ApplicationStorage().GetSettingFromStorage<int>(Constants.ApplicationStorage.Settings.PollingInterval);

            if (pollingInterval > 0)
            {
                lock (_lock)
                {
                    _pollingTimer?.Dispose();
                    _pollingTimer = new Timer(
                        _ => _ = PollOrchestrationAsync(),
                        null,
                        TimeSpan.FromMinutes(pollingInterval),
                        TimeSpan.FromMinutes(pollingInterval)
                    );
                }

                Log.Information("OrchestrationUpdateTask registered with interval {PollingInterval} minutes", pollingInterval);
                return Task.FromResult(true);
            }

            Log.Warning("OrchestrationUpdateTask not registered - polling interval is 0 or not set");
            return Task.FromResult(false);
        }

        private static async Task PollOrchestrationAsync()
        {
            try
            {
                Log.Information("OrchestrationUpdateTask polling for orchestration updates");
                await Orchestrator.GetNextOrchestration(new HttpHelper(), new ApplicationStorage());
                Log.Information("OrchestrationUpdateTask poll completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OrchestrationUpdateTask poll failed");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                UnregisterOrchestrationUpdater();
                _isDisposed = true;
            }
        }
    }
}
