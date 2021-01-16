/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.PageArguments;
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
        private MainPageArguments _currentPageArguments = null;
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

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => _currentPageArguments = e.Parameter as MainPageArguments;

        protected override void OnNavigatedFrom(NavigationEventArgs e) => _loadCompletionTime.Stop();

        private async void _loadCompletionTime_Tick(object sender, object e)
        {
            _loadCompletionTime.Stop();
            await _orchestrator.StartOrchestration();
        }

        private void EvaluateMainPageArguments(MainPageArguments mainPageArguments)
        {
            if (mainPageArguments != null && mainPageArguments is MainPageArguments)
                if (mainPageArguments.ShowSetupInformation)
                    Frame.Navigate(typeof(Settings));
                else
                    return;
            else
            {
                _orchestrator = new Orchestrator();
                _orchestrator.OrchestrationStarted += RegisterUpdater;
                _orchestrator.OrchestrationInvalid += OrchestrationInvalid;
                _orchestrator.NextAction += _orchestrator_NextAction;
                _orchestrator.OrchestrationCancelled += _orchestrator_OrchestrationCancelled;

                _loadCompletionTime.Start();
            }
        }

        private void _orchestrator_OrchestrationCancelled(string reason)
        {
            Frame.Navigate(typeof(Settings), new SettingsPageArguments(new List<string>() { reason }));
        }

        private void _orchestrator_NextAction(Actions.Action action)
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

        private async void RegisterUpdater()
        {
            // Start background task to poll for next OrchestrationInstance
            await OrchestrationPollingManager.OrchestrationUpdateTask.RegisterOrchestrationInstanceUpdater();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            EvaluateMainPageArguments(_currentPageArguments);
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            _loadCompletionTime.Stop();
            Frame.Navigate(typeof(Settings));
        }
    }
}
