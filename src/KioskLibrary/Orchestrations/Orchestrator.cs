/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Action = KioskLibrary.Actions.Action;
using KioskLibrary.Storage;
using KioskLibrary.Orchestrations;
using Windows.ApplicationModel.Core;
using KioskLibrary.Helpers;
using Serilog;

namespace KioskLibrary.Orchestrations
{
    /// <summary>
    /// Processes an <see cref="Orchestration" />
    /// </summary>
    public class Orchestrator
    {
        private Orchestration _orchestration;
        private readonly ITimeHelper _durationtimer;
        private Action _currentAction;
        private List<Action> _orchestrationSequence;
        private readonly IHttpHelper _httpHelper;
        private readonly IApplicationStorage _applicationStorage;

        /// <summary>
        /// Delegate used for <see cref="OrchestrationValidationComplete" />
        /// </summary>
        public delegate void OrchestrationValidationCompleteDelegate(Orchestration orchestration);

        /// <summary>
        /// Event that's raised when the orchestration validation is complete
        /// </summary>
        public event OrchestrationValidationCompleteDelegate OrchestrationValidationComplete;

        /// <summary>
        /// Delegate used for <see cref="OrchestrationStarted" />
        /// </summary>
        public delegate void OrchestrationStartedDelegate();

        /// <summary>
        /// Event that's raised when the orchestration starts
        /// </summary>
        public event OrchestrationStartedDelegate OrchestrationStarted;

        /// <summary>
        /// The delegate for receiving <see cref="NextAction" /> events
        /// </summary>
        /// <param name="action">The next action</param>
        public delegate void NextActionDelegate(Action action);

        /// <summary>
        /// Event that's fired when the orchestrator is ready to display a new action
        /// </summary>
        public event NextActionDelegate NextAction;

        /// <summary>
        /// The delegate for receiving <see cref="OrchestrationCancelled" /> events
        /// </summary>
        /// <param name="reason">The reason for the cancellation</param>
        public delegate void OrchestrationCancelledDelegate(string reason);

        /// <summary>
        /// Event that's fired when an orchestration has been cancelled
        /// </summary>
        public event OrchestrationCancelledDelegate OrchestrationCancelled;

        /// <summary>
        /// The delegate for receiving <see cref="OrchestrationStatusUpdate" /> events
        /// </summary>
        /// <param name="status">The status of the Orchestration</param>
        public delegate void OrchestrationStatusUpdateDelegate(string status);

        /// <summary>
        /// Event that's fired when an Orchestration's status has updated
        /// </summary>
        public event OrchestrationStatusUpdateDelegate OrchestrationStatusUpdate;

        /// <summary>
        /// The delegate for receiving <see cref="OrchestrationStatusUpdate" /> events
        /// </summary>
        /// <param name="orchestration">The <see cref="Orchestration"/> that was loaded</param>
        public delegate void OrchestrationLoadeedDelegate(Orchestration orchestration);

        /// <summary>
        /// Event that's fired when an Orchestration is loaded
        /// </summary>
        public event OrchestrationLoadeedDelegate OrchestrationLoaded;

        /// <summary>
        /// Constructor
        /// </summary>
        public Orchestrator(ITimeHelper timeHelper = null)
        {
            if (_httpHelper == null)
                _httpHelper = new HttpHelper();

            if (_applicationStorage == null)
                _applicationStorage = new ApplicationStorage();

            _orchestrationSequence = new List<Action>();

            _orchestration = null;
            _currentAction = null;

            _durationtimer = timeHelper ?? new TimerHelper();
            _durationtimer.Tick += Durationtime_Tick;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        /// <param name="applicationStorage">The <see cref="IApplicationStorage"/> to use for interacting with local application storage</param>
        /// <param name="timeHelper">The <see cref="ITimeHelper"/> to use for interacting with the <see cref="DispatcherTimer"/></param>
        public Orchestrator(IHttpHelper httpHelper, IApplicationStorage applicationStorage, ITimeHelper timeHelper)
            : this(timeHelper)
        {
            _httpHelper = httpHelper;
            _applicationStorage = applicationStorage;
        }

        /// <summary>
        /// Gets the updated <see cref="Orchestration" /> from the web
        /// </summary>
        public static async Task GetNextOrchestration(IHttpHelper httpHelper, IApplicationStorage applicationStorage)
        {
            Log.Information("GetNextOrchestration invoked");

            // Get the Settings URI
            var currentOrchestrationURI = applicationStorage.GetSettingFromStorage<string>(Constants.ApplicationStorage.Settings.DefaultOrchestrationURI);

            if (!string.IsNullOrEmpty(currentOrchestrationURI))
            {
                Log.Information("GetNextOrchestration - currentOrchestrationURI: {uri}", currentOrchestrationURI);

                // Get orchestration from Settings URI
                var nextOrchestration = await Orchestration.GetOrchestration(new Uri(currentOrchestrationURI), httpHelper);

                Log.Information("GetNextOrchestration - nextOrchestration: {nextOrchestration}", SerializationHelper.JSONSerialize(nextOrchestration));

                // Save to the 'NextOrchestration'
                await applicationStorage.SaveFileToStorageAsync(Constants.ApplicationStorage.Files.NextOrchestration, nextOrchestration);
            }
        }

        /// <summary>
        /// Starts processing the current <see cref="Orchestration" />
        /// </summary>
        public async Task StartOrchestration()
        {
            Log.Information("StartOrchestration invoked");

            OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.Initializing);

            _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.EndOrchestration, false); //Ensure the EndOrchestration value has been reset to false

            var orchestrationSource = _applicationStorage.GetSettingFromStorage<OrchestrationSource>(Constants.ApplicationStorage.Settings.DefaultOrchestrationSource);

            OrchestrationStatusUpdate?.Invoke($"{Constants.Orchestrator.StatusMessages.Loading} {orchestrationSource}");

            _orchestration = await LoadOrchestration(orchestrationSource, _httpHelper, _applicationStorage);

            if (_orchestration != null)
            {
                OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.LoadedSuccessfully);

                _orchestration.OrchestrationSource = orchestrationSource;
                OrchestrationLoaded?.Invoke(_orchestration);

                OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.ValidatingOrchestration);

                await _orchestration.ValidateAsync();
                OrchestrationValidationComplete?.Invoke(_orchestration);

                Log.Information("StartOrchestration - _orchestration validation: {status} | {errors}", _orchestration.IsValid, _orchestration.ValidationResult);

                if (_orchestration.IsValid)
                {
                    OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.OrchestrationValid);

                    if (orchestrationSource == OrchestrationSource.URL)
                    {
                        OrchestrationStatusUpdate?.Invoke($"{Constants.Orchestrator.StatusMessages.SettingPollingInterval} {_orchestration.PollingIntervalMinutes} minutes");
                        _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.PollingInterval, _orchestration.PollingIntervalMinutes);
                    }

                    OrchestrationStarted?.Invoke();

                    try
                    {
                        ApplicationView.GetForCurrentView()?.TryEnterFullScreenMode();
                    }
                    catch (Exception ex) { Log.Error(ex, ex.Message); }

                    if (_orchestration.Actions.Any())
                    {
                        OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.SettingActionSequence);

                        // Populate the _orchestrationSequence with a sequence of actions to take
                        Log.Information("Action order set to {order}", _orchestration.Order);
                        if (_orchestration.Order == Ordering.Sequential)
                        {
                            _orchestrationSequence = new List<Action>(_orchestration.Actions);

                            foreach (var action in _orchestration.Actions)
                                Log.Information("Adding action: {action}", action);
                        }
                        else
                            PopulateRandomSequenceOfActions(new List<Action>(_orchestration.Actions));

                        await EvaluateNextAction();
                    }
                }
                else
                {
                    OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.OrchestrationInvalid);
                    return;
                }
            }
            else
                StopOrchestration(Constants.Orchestrator.StatusMessages.NoValidOrchestration);
        }

        /// <summary>
        /// Stop processing the current <see cref="Orchestration" />
        /// </summary>
        public void StopOrchestration(string message)
        {
            Log.Information("StopOrchestration - Stopping orchestration");

            if (_durationtimer != null)
                _durationtimer.Stop();

            OrchestrationCancelled?.Invoke(message);
        }

        private static async Task<Orchestration> LoadOrchestration(OrchestrationSource orchestrationSource, IHttpHelper httpHelper, IApplicationStorage applicationStorage)
        {
            Log.Information("LoadOrchestration invoked: {source}", orchestrationSource);

            Orchestration toReturn = null;

            if (orchestrationSource == OrchestrationSource.File) // Load Orchestration from Storage.
                toReturn = await applicationStorage.GetFileFromStorageAsync<Orchestration>(Constants.ApplicationStorage.Files.DefaultOrchestration);
            else if (orchestrationSource == OrchestrationSource.URL) // Load Orchestration from the web.
            {
                var orchestrationPath = applicationStorage.GetSettingFromStorage<string>(Constants.ApplicationStorage.Settings.DefaultOrchestrationURI);
                if (!string.IsNullOrEmpty(orchestrationPath)) // We are pulling from a URL
                    if (Uri.TryCreate(orchestrationPath, UriKind.Absolute, out var OrchestrationUri))
                        toReturn = await Orchestration.GetOrchestration(OrchestrationUri, httpHelper); // Pull a new instace from the URL
            }

            Log.Information("LoadOrchestration - result: {orchestration}", SerializationHelper.JSONSerialize(toReturn));

            return toReturn;
        }

        private void PopulateRandomSequenceOfActions(List<Action> remainingActions)
        {
            if (remainingActions.Any())
                if (remainingActions.Count == 1)
                {
                    var action = remainingActions.First();
                    _orchestrationSequence.Add(action);
                    Log.Information("Adding action: {action}", action);
                }
                else
                {
                    var action = remainingActions[new Random().Next(remainingActions.Count - 1)];
                    _orchestrationSequence.Add(action);
                    Log.Information("Adding action: {action}", action);
                    remainingActions.Remove(action);
                    PopulateRandomSequenceOfActions(remainingActions);
                }
        }

        private async Task EvaluateNextAction()
        {
            var endOrchestration = _applicationStorage.GetSettingFromStorage<bool>(Constants.ApplicationStorage.Settings.EndOrchestration);
            if (endOrchestration)
            {
                Log.Information("EvaluateNextAction - Orchestration ending");

                _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.EndOrchestration, false);
                StopOrchestration(Constants.Orchestrator.StatusMessages.OrchestrationEnded);
                return;
            }

            if (_orchestrationSequence.Any()) // We have actions to execute
            {
                if (_currentAction == null)
                    _currentAction = _orchestrationSequence[0];
                else
                {
                    var index = _orchestrationSequence.IndexOf(_currentAction);
                    if (index + 1 < _orchestrationSequence.Count)
                        _currentAction = _orchestrationSequence[index + 1];
                }

                OrchestrationStatusUpdate?.Invoke($"{Constants.Orchestrator.StatusMessages.StartingAction} {_currentAction.Name}");

                // After we have set the _currentAction, we can assess the duration
                if (_currentAction.Duration != null && _currentAction.Duration.HasValue)
                {
                    _durationtimer.Stop();
                    _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.EndOrchestration, false);
                    _durationtimer.Interval = TimeSpan.FromSeconds(_currentAction.Duration.Value);
                    _durationtimer.Start();
                }

                _orchestrationSequence.Remove(_currentAction); // Remove the current action from the sequence of actions

                Log.Information("EvaluateNextAction - Calling next action: {action}", _currentAction.ToString());

                NextAction?.Invoke(_currentAction);
            }
            else if (_orchestration.Lifecycle == LifecycleBehavior.ContinuousLoop) // We don't have any more actions to execute, so we see if we need to start over
            {
                Log.Information(Constants.Orchestrator.StatusMessages.RestartingOrchestration);
                OrchestrationStatusUpdate?.Invoke(Constants.Orchestrator.StatusMessages.RestartingOrchestration);
                await StartOrchestration();
            }
            else // We don't have any actions to execute and we are not instructed to start over
            {
                Log.Information(Constants.Orchestrator.StatusMessages.ExitingApplication);
                CoreApplication.Exit();
            }
        }

        private async void Durationtime_Tick(object sender, object e) => await EvaluateNextAction();
    }
}
