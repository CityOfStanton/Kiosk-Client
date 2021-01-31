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

namespace KioskLibrary
{
    /// <summary>
    /// Processes an <see cref="OrchestrationInstance" />
    /// </summary>
    public class Orchestrator
    {
        private OrchestrationInstance _orchestrationInstance;
        private readonly DispatcherTimer _durationtime;
        private int _durationCounter;
        private Action _currentAction;
        private List<Action> _orchestrationSequence;
        private IHttpHelper _httpHelper;


        /// <summary>
        /// The delegate for receiving <see cref="Orchestrator.NextAction" /> events
        /// </summary>
        /// <param name="action">The next action</param>
        public delegate void NextActionDelegate(Action action);

        /// <summary>
        /// Event that's fired when the orchestrator is ready to display a new action
        /// </summary>
        public event NextActionDelegate NextAction;

        /// <summary>
        /// The delegate for receiving <see cref="Orchestrator.OrchestrationCancelled" /> events
        /// </summary>
        /// <param name="reason">The reason for the cancellation</param>
        public delegate void OrchestrationCancelledDelegate(string reason);

        /// <summary>
        /// Event that's fired when an orchestration has been cancelled
        /// </summary>
        public event OrchestrationCancelledDelegate OrchestrationCancelled;

        /// <summary>
        /// Delegate used for <see cref="Orchestrator.OrchestrationStarted" />
        /// </summary>
        public delegate void OrchestrationStartedDelegate();

        /// <summary>
        /// Delegate used for <see cref="Orchestrator.OrchestrationInvalid" />
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
        public Orchestrator()
        {
            _orchestrationInstance = null;
            _currentAction = null;

            _durationCounter = 0;

            _durationtime = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0)
            };
            _durationtime.Tick += Durationtime_Tick;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        public Orchestrator(IHttpHelper httpHelper)
            : this()
        {
            _httpHelper = httpHelper;
        }

        /// <summary>
        /// Gets the updated <see cref="OrchestrationInstance" /> from the web
        /// </summary>
        public static async Task GetNextOrchestration(IHttpHelper httpHelper)
        {
            // Get the Settings URI
            var currentOrchestrationURI = ApplicationStorage.GetFromStorage<string>(Constants.ApplicationStorage.CurrentOrchestrationURI);

            if (!string.IsNullOrEmpty(currentOrchestrationURI))
            {
                // Get orchestration from Settings URI
                var nextOrchestration = await OrchestrationInstance.GetOrchestrationInstance(new Uri(currentOrchestrationURI), httpHelper);

                // Save to the 'NextOrchestration'
                ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.NextOrchestration, nextOrchestration);
            }
        }

        /// <summary>
        /// Starts processing the current <see cref="OrchestrationInstance" />
        /// </summary>
        /// <returns></returns>
        public async Task StartOrchestration()
        {
            var orchestrationSource = ApplicationStorage.GetFromStorage<OrchestrationSource>(Constants.ApplicationStorage.CurrentOrchestrationSource);

            _durationCounter = 0;

            _orchestrationInstance = await LoadOrchestration(orchestrationSource, _httpHelper);

            if (_orchestrationInstance != null)
            {
                _orchestrationInstance.OrchestrationSource = orchestrationSource;

                (bool status, List<string> errors) = await _orchestrationInstance.ValidateAsync();

                if (!status)
                {
                    OrchestrationInvalid?.Invoke(errors);

                    return;
                }

                if (orchestrationSource == OrchestrationSource.URL)
                    ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.PollingInterval, _orchestrationInstance.PollingIntervalMinutes);

                OrchestrationStarted?.Invoke();

                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

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
                OrchestrationCancelled?.Invoke("No valid orchestration was loaded.");
        }

        /// <summary>
        /// Stop processing the current <see cref="OrchestrationInstance" />
        /// </summary>
        public void StopOrchestration()
        {
            if (_durationtime != null)
                _durationtime.Stop();
        }

        private static async Task<OrchestrationInstance> LoadOrchestration(OrchestrationSource orchestrationSource, IHttpHelper httpHelper)
        {
            OrchestrationInstance toReturn = null;

            if (orchestrationSource == OrchestrationSource.File) // Load OrchestrationInstance from Storage.
                toReturn = ApplicationStorage.GetFromStorage<OrchestrationInstance>(Constants.ApplicationStorage.CurrentOrchestration);
            else if (orchestrationSource == OrchestrationSource.URL) // Load OrchestrationInstance from the web.
            {
                var orchestrationInstancePath = ApplicationStorage.GetFromStorage<string>(Constants.ApplicationStorage.CurrentOrchestrationURI);
                if (!string.IsNullOrEmpty(orchestrationInstancePath)) // We are pulling from a URL
                    if (Uri.TryCreate(orchestrationInstancePath, UriKind.Absolute, out var OrchestrationInstanceUri))
                        toReturn = await OrchestrationInstance.GetOrchestrationInstance(OrchestrationInstanceUri, httpHelper); // Pull a new instace from the URL
            }

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
            var endOrchestration = ApplicationStorage.GetFromStorage<bool>(Constants.ApplicationStorage.EndOrchestration);
            if (endOrchestration)
            {
                ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.EndOrchestration, false);
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
                    ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.EndOrchestration, false);
                    _durationCounter = 0;
                    _durationtime.Start();
                }

                _orchestrationSequence.Remove(_currentAction); // Remove the current action from the sequence of actions

                NextAction?.Invoke(_currentAction);
            }
            else if (_orchestrationInstance.Lifecycle == LifecycleBehavior.ContinuousLoop) // We don't have any more actions to execute, so we see if we need to start over
                await StartOrchestration();
            else // We don't have any actions to execute and we are not instructed to start over
                CoreApplication.Exit();
        }

        private async void Durationtime_Tick(object sender, object e)
        {
            if (_currentAction?.Duration != null && _currentAction.Duration.HasValue == true)
                if (_durationCounter >= _currentAction.Duration.Value)
                    await EvaluateNextAction();

            _durationCounter++;
        }
    }
}
