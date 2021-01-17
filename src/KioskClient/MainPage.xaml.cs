/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Pages;
using KioskLibrary.Pages.Actions;
using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using KioskClient.Pages;
using KioskClient.Pages.PageArguments;

namespace KioskLibrary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool? _loadSettings = null;
        private DispatcherTimer _loadCompletionTime;
        private Orchestrator _orchestrator;
        private Dictionary<Type, Type> _actionToFrameMap;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp; // Remove any pre-existing Common.CommonKeyUp handlers
            Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp; // Add a single Common.CommonKeyUp handler

            _loadCompletionTime = new DispatcherTimer();
            _loadCompletionTime.Interval = TimeSpan.FromSeconds(2);
            _loadCompletionTime.Tick += _loadCompletionTime_Tick;

            _actionToFrameMap = new Dictionary<Type, Type>();
            _actionToFrameMap.Add(typeof(ImageAction), typeof(ImagePage));
            _actionToFrameMap.Add(typeof(WebsiteAction), typeof(WebsitePage));

            _orchestrator = new Orchestrator();
            _orchestrator.OrchestrationStarted += OrchestrationStarted;
            _orchestrator.OrchestrationInvalid += OrchestrationInvalid;
            _orchestrator.NextAction += NextAction;
            _orchestrator.OrchestrationCancelled += OrchestrationCancelled;

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => _loadSettings = e.Parameter as bool?;

        protected override void OnNavigatedFrom(NavigationEventArgs e) => _loadCompletionTime.Stop();

        private async void _loadCompletionTime_Tick(object sender, object e)
        {
            _loadCompletionTime.Stop();
            await _orchestrator.StartOrchestration();
        }

        private void OrchestrationCancelled(string reason)
        {
            Frame.Navigate(typeof(Settings), new SettingsPageArguments(new List<string>() { reason }));
        }

        private void NextAction(Actions.Action action)
        {
            Type nextPage;

            if (_actionToFrameMap.ContainsKey(action.GetType()))
                nextPage = _actionToFrameMap[action.GetType()];
            else
                throw new NotSupportedException($"There is no corresponding Page mapped to [{action.GetType().Name}]");

            Frame.Navigate(nextPage, action);
        }

        private void OrchestrationInvalid(List<string> errors)
        {
            Frame.Navigate(typeof(Settings), new SettingsPageArguments(errors));
        }

        private async void OrchestrationStarted()
        {
            // Start background task to poll for next OrchestrationInstance
            await OrchestrationPollingManager.OrchestrationUpdateTask.RegisterOrchestrationInstanceUpdater();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _loadCompletionTime.Start();

            if (_loadSettings.HasValue && _loadSettings.Value)
            {
                _loadCompletionTime.Start();
                Frame.Navigate(typeof(Settings));
            }
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            _loadCompletionTime.Stop();
            Frame.Navigate(typeof(Settings));
        }
    }
}
