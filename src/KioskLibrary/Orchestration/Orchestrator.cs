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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Action = KioskLibrary.Actions.Action;
using KioskLibrary.Storage;
using KioskLibrary.DataSerialization;
using KioskLibrary.Orchestration;
using Windows.ApplicationModel.Core;
using KioskLibrary.PageArguments;

namespace KioskLibrary
{
    public class Orchestrator
    {
        private Dictionary<Type, Type> _actionToFrameMap;
        private Type _settingsPage;
        private Frame _rootFrame;
        private OrchestrationInstance _orchestrationInstance;
        private DispatcherTimer _durationtime;
        private int _durationCounter;
        private Action _currentAction;
        private List<Action> _orchestrationSequence;

        public delegate void OrchestrationStartedDelegate();
        public delegate void OrchestrationInvalidDelegate(List<string> errors);

        public event OrchestrationStartedDelegate OrchestrationStarted;
        public event OrchestrationInvalidDelegate OrchestrationInvalid;

        public Orchestrator(Type settings, Dictionary<Type, Type> actionToFrameMap, Frame rootFrame)
        {
            _actionToFrameMap = actionToFrameMap;
            _settingsPage = settings;
            _rootFrame = rootFrame;

            _orchestrationInstance = null;
            _currentAction = null;

            _durationCounter = 0;

            _durationtime = new DispatcherTimer();
            _durationtime.Interval = TimeSpan.FromSeconds(1.0);
            _durationtime.Tick += Durationtime_Tick;
        }

        public async static Task<OrchestrationInstance> GetOrchestrationInstance(Uri uri)
        {
            try
            {
                var client = new HttpClient();
                var result = await client.GetAsync(uri);
                if (result.StatusCode == HttpStatusCode.Ok)
                    return ConvertStringToOrchestrationInstance(await result.Content.ReadAsStringAsync());
            }
            catch { }

            return null;
        }

        public static OrchestrationInstance ConvertStringToOrchestrationInstance(string body)
        {
            try
            {
                // Try to parse the text as JSON
                return Serialization.Deserialize<OrchestrationInstance>(body);
            }
            catch (JsonException)
            {
                // Try to parse the text as XML
                using var sr = new StringReader(body);
                try
                {
                    return new XmlSerializer(typeof(OrchestrationInstance)).Deserialize(sr) as OrchestrationInstance;
                }
                catch { }
                finally
                {
                    sr.Close();
                }
            }

            return null;
        }

        public static async Task GetNextOrchestration()
        {
            // Get the Settings URI
            var currentOrchestrationURI = ApplicationStorage.GetFromStorage<string>(Constants.CurrentOrchestrationURI);

            if (!string.IsNullOrEmpty(currentOrchestrationURI))
            {
                // Get orchestration from Settings URI
                var nextOrchestration = await GetOrchestrationInstance(new Uri(currentOrchestrationURI));

                // Save to the 'NextOrchestration'
                ApplicationStorage.SaveToStorage(Constants.NextOrchestration, nextOrchestration);
            }
        }

        public async Task StartOrchestration()
        {
            var orchestrationSource = ApplicationStorage.GetFromStorage<OrchestrationSource>(Constants.CurrentOrchestrationSource);

            _durationCounter = 0;

            _orchestrationInstance = await LoadOrchestration(orchestrationSource);

            if (_orchestrationInstance != null)
            {
                _orchestrationInstance.OrchestrationSource = orchestrationSource;

                (bool status, List<string> errors) = await _orchestrationInstance.ValidateAsync();

                if (!status)
                {
                    if (OrchestrationInvalid != null)
                        OrchestrationInvalid(errors);

                    return;
                }

                if (orchestrationSource == OrchestrationSource.URL)
                    ApplicationStorage.SaveToStorage(Constants.PollingInterval, _orchestrationInstance.PollingIntervalMinutes);

                if (OrchestrationStarted != null)
                    OrchestrationStarted();

                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

                if (_orchestrationInstance.Actions.Any())
                {
                    // Populate the _orchestrationSequence with a sequrnce of actions to take
                    if (_orchestrationInstance.Order == Ordering.Sequential)
                        _orchestrationSequence = new List<Action>(_orchestrationInstance.Actions);
                    else
                        PopulateRandomSequenceOfActions(new List<Action>(_orchestrationInstance.Actions));

                    await EvaluateNextAction();
                }
            }
            else
                _rootFrame.Navigate(_settingsPage, new SettingsPageArguments(new List<string>() { "No valid orchestration was loaded." }));
        }

        public void StopOrchestration()
        {
            if (_durationtime != null)
                _durationtime.Stop();
        }

        private static async Task<OrchestrationInstance> LoadOrchestration(OrchestrationSource orchestrationSource)
        {
            OrchestrationInstance toReturn = null;

            if (orchestrationSource == OrchestrationSource.File) // Load OrchestrationInstance from Storage.
                toReturn = ApplicationStorage.GetFromStorage<OrchestrationInstance>(Constants.CurrentOrchestration);
            else if (orchestrationSource == OrchestrationSource.URL) // Load OrchestrationInstance from the web.
            {
                var orchestrationInstancePath = ApplicationStorage.GetFromStorage<string>(Constants.CurrentOrchestrationURI);
                if (!string.IsNullOrEmpty(orchestrationInstancePath)) // We are pulling from a URL
                    if (Uri.TryCreate(orchestrationInstancePath, UriKind.Absolute, out var OrchestrationInstanceUri))
                        toReturn = await GetOrchestrationInstance(OrchestrationInstanceUri); // Pull a new instace from the URL
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
            var endOrchestration = ApplicationStorage.GetFromStorage<bool>(Constants.EndOrchestration);
            if (endOrchestration)
            {
                ApplicationStorage.SaveToStorage(Constants.EndOrchestration, false);
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
                    ApplicationStorage.SaveToStorage(Constants.EndOrchestration, false);
                    _durationCounter = 0;
                    _durationtime.Start();
                }

                _orchestrationSequence.Remove(_currentAction); // Remove the current action from the sequence of actions

                NavigateToNextActionPage(_currentAction);
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

        private async void PollingTime_Tick(object sender, object e)
        {
            var nextOrchestrationInstance = ApplicationStorage.GetFromStorage<OrchestrationInstance>(Constants.NextOrchestration);

            if (nextOrchestrationInstance != null)
            {
                var currentOrchestrationInstance = ApplicationStorage.GetFromStorage<OrchestrationInstance>(Constants.CurrentOrchestration);

                var serializedNextOrchestrationInstance = Serialization.Serialize(nextOrchestrationInstance);
                var serializedCurrentOrchestrationInstance = Serialization.Serialize(currentOrchestrationInstance);

                ApplicationStorage.ClearItemFromStorage(Constants.NextOrchestration); // Clear the NextOrchestration

                if (serializedNextOrchestrationInstance != serializedCurrentOrchestrationInstance)
                    await StartOrchestration();
            }
        }

        private void NavigateToNextActionPage(Action action)
        {
            Type nextPage;

            if (_actionToFrameMap.ContainsKey(action.GetType()))
                nextPage = _actionToFrameMap[action.GetType()];
            else
                throw new NotSupportedException($"There is no corresponding Page mapped to [{action.GetType().Name}]");

            _rootFrame.Navigate(nextPage, action);
        }
    }
}
