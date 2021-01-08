using KioskLibrary.Actions;
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

namespace KioskLibrary
{
    public class Orchestrator
    {
        private Dictionary<Type, Type> _actionToFrameMap;
        private Type _settingsPage;
        private Frame _rootFrame;
        private OrchestrationInstance _orchestrationInstance;
        private DispatcherTimer _durationtime;
        private DispatcherTimer _pollingTimer;
        private int _durationCounter;
        private Action _currentAction;
        private List<Action> _orchestrationSequence;

        public delegate void OrchestrationStartedDelegate();

        public event OrchestrationStartedDelegate OrchestrationStarted;

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

            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromSeconds(30.0);
            _pollingTimer.Tick += PollingTime_Tick;
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

        public async static Task<(bool, string)> VerifyOrchestrationInstance(string settingsUri, HttpStatusCode expectedResult)
        {
            try
            {
                var uri = new Uri(settingsUri);
                var client = new HttpClient();
                var result = await client.GetAsync(uri);

                return (result.StatusCode == expectedResult, "URL is valid!");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
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
            _durationCounter = 0;

            await LoadOrchestration();

            // Invoke the OrchestrationInstance
            if (_orchestrationInstance != null)
            {
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
                _rootFrame.Navigate(_settingsPage);
        }

        private async Task LoadOrchestration()
        {
            // Load OrchestrationInstance from Storage. If we're loading from a file, this will be the Orchestration we will use.
            _orchestrationInstance = ApplicationStorage.GetFromStorage<OrchestrationInstance>(Constants.CurrentOrchestration);

            var orchestrationInstancePath = ApplicationStorage.GetFromStorage<string>(Constants.CurrentOrchestrationURI);

            // If we are pulling from a URL, pull a new OrchestrationInstance and set up polling
            // If we are pulling from a file, use the OrchestrationInstance from Storage
            if (!string.IsNullOrEmpty(orchestrationInstancePath)) // We are pulling from a URL
                if (Uri.TryCreate(orchestrationInstancePath, UriKind.Absolute, out var OrchestrationInstanceUri))
                    _orchestrationInstance = await GetOrchestrationInstance(OrchestrationInstanceUri); // Pull a new instace from the URL

            // We don't need the 'else' here since this case indicates that we have completed our Lifecycle before we have polled for a new Orchestration instance
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

        private void Durationtime_Tick(object sender, object e)
        {
            if (_currentAction?.Duration != null && _currentAction.Duration.HasValue == true)
                if (_durationCounter >= _currentAction.Duration.Value)
                    EvaluateNextAction().Wait();

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
