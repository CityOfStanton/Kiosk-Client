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
using Windows.UI.Xaml;
using Action = KioskLibrary.Actions.Action;
using KioskLibrary.Storage;
using KioskLibrary.Orchestration;
using Windows.ApplicationModel.Core;
using KioskLibrary.Helpers;
using Serilog;

namespace KioskLibrary
{
    /// <summary>
    /// Processes an <see cref="OrchestrationInstance" />
    /// </summary>
    public class Orchestrator
    {
        private OrchestrationInstance _orchestrationInstance;
        private readonly ITimeHelper _durationtimer;
        private Action _currentAction;
        private List<Action> _orchestrationSequence;
        private readonly IHttpHelper _httpHelper;
        private readonly IApplicationStorage _applicationStorage;

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
        /// Delegate used for <see cref="OrchestrationStarted" />
        /// </summary>
        public delegate void OrchestrationStartedDelegate();

        /// <summary>
        /// Delegate used for <see cref="OrchestrationInvalid" />
        /// </summary>
        public delegate void OrchestrationInvalidDelegate(List<string> errors);

        /// <summary>
        /// Event that's raised when the orchestration starts
        /// </summary>
        public event OrchestrationStartedDelegate OrchestrationStarted;

        /// <summary>
        /// Event that's raised when the orchestration is invalid
        /// </summary>
        public event OrchestrationInvalidDelegate OrchestrationInvalid;

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

            _orchestrationInstance = null;
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
        /// Gets the updated <see cref="OrchestrationInstance" /> from the web
        /// </summary>
        public static async Task GetNextOrchestration(IHttpHelper httpHelper, IApplicationStorage applicationStorage)
        {
            Log.Information("GetNextOrchestration invoked");

            // Get the Settings URI
            var currentOrchestrationURI = applicationStorage.GetFromStorage<string>(Constants.ApplicationStorage.DefaultOrchestrationURI);

            if (!string.IsNullOrEmpty(currentOrchestrationURI))
            {
                Log.Information("GetNextOrchestration - currentOrchestrationURI: {uri}", currentOrchestrationURI);

                // Get orchestration from Settings URI
                var nextOrchestration = await OrchestrationInstance.GetOrchestrationInstance(new Uri(currentOrchestrationURI), httpHelper);

                Log.Information("GetNextOrchestration - nextOrchestration: {nextOrchestration}", SerializationHelper.JSONSerialize(nextOrchestration));

                // Save to the 'NextOrchestration'
                applicationStorage.SaveToStorage(Constants.ApplicationStorage.NextOrchestration, nextOrchestration);
            }
        }

        /// <summary>
        /// Starts processing the current <see cref="OrchestrationInstance" />
        /// </summary>
        public async Task StartOrchestration()
        {
            Log.Information("StartOrchestration invoked");

            var orchestrationSource = _applicationStorage.GetFromStorage<OrchestrationSource>(Constants.ApplicationStorage.DefaultOrchestrationSource);

            _orchestrationInstance = await LoadOrchestration(orchestrationSource, _httpHelper, _applicationStorage);

            if (_orchestrationInstance != null)
            {
                _orchestrationInstance.OrchestrationSource = orchestrationSource;

                (bool status, List<string> errors) = await _orchestrationInstance.ValidateAsync();

                Log.Information("StartOrchestration - _orchestrationInstance validation: {status} | {errors}", status, errors);

                if (!status)
                {
                    OrchestrationInvalid?.Invoke(errors);
                    return;
                }

                if (orchestrationSource == OrchestrationSource.URL)
                    _applicationStorage.SaveToStorage(Constants.ApplicationStorage.PollingInterval, _orchestrationInstance.PollingIntervalMinutes);

                OrchestrationStarted?.Invoke();

                try
                {
                    ApplicationView.GetForCurrentView()?.TryEnterFullScreenMode();
                }
                catch (Exception ex) { Log.Error(ex, ex.Message); }

                if (_orchestrationInstance.Actions.Any())
                {
                    // Populate the _orchestrationSequence with a sequence of actions to take
                    if (_orchestrationInstance.Order == Ordering.Sequential)
                        _orchestrationSequence = new List<Action>(_orchestrationInstance.Actions);
                    else
                        PopulateRandomSequenceOfActions(new List<Action>(_orchestrationInstance.Actions));

                    await EvaluateNextAction();
                }
            }
            else
                OrchestrationCancelled?.Invoke(Constants.Orchestrator.NoValidOrchestration);
        }

        /// <summary>
        /// Stop processing the current <see cref="OrchestrationInstance" />
        /// </summary>
        public void StopOrchestration()
        {
            Log.Information("StopOrchestration - Stopping orchestration");

            if (_durationtimer != null)
                _durationtimer.Stop();
        }

        private static async Task<OrchestrationInstance> LoadOrchestration(OrchestrationSource orchestrationSource, IHttpHelper httpHelper, IApplicationStorage applicationStorage)
        {
            Log.Information("LoadOrchestration invoked: {source}", orchestrationSource);

            OrchestrationInstance toReturn = null;

            if (orchestrationSource == OrchestrationSource.File) // Load OrchestrationInstance from Storage.
                toReturn = applicationStorage.GetFromStorage<OrchestrationInstance>(Constants.ApplicationStorage.DefaultOrchestration);
            else if (orchestrationSource == OrchestrationSource.URL) // Load OrchestrationInstance from the web.
            {
                var orchestrationInstancePath = applicationStorage.GetFromStorage<string>(Constants.ApplicationStorage.DefaultOrchestrationURI);
                if (!string.IsNullOrEmpty(orchestrationInstancePath)) // We are pulling from a URL
                    if (Uri.TryCreate(orchestrationInstancePath, UriKind.Absolute, out var OrchestrationInstanceUri))
                        toReturn = await OrchestrationInstance.GetOrchestrationInstance(OrchestrationInstanceUri, httpHelper); // Pull a new instace from the URL
            }

            Log.Information("LoadOrchestration - result: {instance}", SerializationHelper.JSONSerialize(toReturn));

            return toReturn;
        }

        private void PopulateRandomSequenceOfActions(List<Action> remainingActions)
        {
            if (remainingActions.Any())
                if (remainingActions.Count == 1)
                    _orchestrationSequence.Add(remainingActions.First());
                else
                {
                    var toAdd = remainingActions[new Random().Next(remainingActions.Count - 1)];
                    _orchestrationSequence.Add(toAdd);
                    remainingActions.Remove(toAdd);
                    PopulateRandomSequenceOfActions(remainingActions);
                }
        }

        private async Task EvaluateNextAction()
        {
            var endOrchestration = _applicationStorage.GetFromStorage<bool>(Constants.ApplicationStorage.EndOrchestration);
            if (endOrchestration)
            {
                Log.Information("EvaluateNextAction - Orchestration ending");

                _applicationStorage.SaveToStorage(Constants.ApplicationStorage.EndOrchestration, false);
                StopOrchestration();
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

                // After we have set the _currentAction, we can assess the duration
                if (_currentAction.Duration != null && _currentAction.Duration.HasValue)
                {
                    _durationtimer.Stop();
                    _applicationStorage.SaveToStorage(Constants.ApplicationStorage.EndOrchestration, false);
                    _durationtimer.Interval = TimeSpan.FromSeconds(_currentAction.Duration.Value);
                    _durationtimer.Start();
                }

                _orchestrationSequence.Remove(_currentAction); // Remove the current action from the sequence of actions

                Log.Information("EvaluateNextAction - Calling next action: {action}", _currentAction.ToString());

                NextAction?.Invoke(_currentAction);
            }
            else if (_orchestrationInstance.Lifecycle == LifecycleBehavior.ContinuousLoop) // We don't have any more actions to execute, so we see if we need to start over
                await StartOrchestration();
            else // We don't have any actions to execute and we are not instructed to start over
                CoreApplication.Exit();
        }

        private async void Durationtime_Tick(object sender, object e) => await EvaluateNextAction();
    }
}
