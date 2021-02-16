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
using KioskClient.Dialogs;
using KioskClient.Pages.PageArguments;
using Serilog;
using Serilog.Formatting.Json;
using Windows.Storage;
using KioskLibrary.Common;

namespace KioskLibrary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool? _loadSettings = null;
        private readonly DispatcherTimer _initializationDelayTimer;
        private readonly DispatcherTimer _progressRing_LoadingTimer;
        private readonly Orchestrator _orchestrator;
        private readonly Dictionary<Type, Type> _actionToFrameMap;
        private readonly List<string> _statusLog;
        private const double _initialLoadTime = 3.0;
        private const double _initialLoadTimeUpdatesPerSecond = 50.0;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            _statusLog = new List<string>();

            var fileSizeLimit = 50 * 1024 * 1024; // 50 MB
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    new JsonFormatter(renderMessage: true),
                    ApplicationData.Current.LocalCacheFolder.Path + "\\log.json",
                    fileSizeLimitBytes: fileSizeLimit,
                    rollingInterval: RollingInterval.Day)
                    .MinimumLevel.Verbose()
                    .CreateLogger();

            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp; // Remove any pre-existing Common.CommonKeyUp handlers
            Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp; // Add a single Common.CommonKeyUp handler

            _initializationDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_initialLoadTime + 0.5)
            };
            _initializationDelayTimer.Tick += InitializationDelayTimer_Tick;

            _progressRing_LoadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds((_initialLoadTime) / _initialLoadTimeUpdatesPerSecond)
            };
            _progressRing_LoadingTimer.Tick += ProgressRing_LoadingTimer_Tick;

            _actionToFrameMap = new Dictionary<Type, Type>
            {
                { typeof(ImageAction), typeof(ImagePage) },
                { typeof(WebsiteAction), typeof(WebsitePage) }
            };

            _orchestrator = new Orchestrator();
            _orchestrator.OrchestrationStarted += OrchestrationStarted;
            _orchestrator.OrchestrationInvalid += OrchestrationInvalid;
            _orchestrator.NextAction += NextAction;
            _orchestrator.OrchestrationCancelled += OrchestrationCancelled;
            _orchestrator.OrchestrationStatusUpdate += OrchestrationStatusUpdate;
            OrchestrationStatusUpdate(Constants.Application.Main.AttemptingToLoadDefaultOrchestration);

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            Log.Information("Kiosk Client started");
        }

        private void ProgressRing_LoadingTimer_Tick(object sender, object e)
        {
            ProgressRing_Loading.Value += (100.0 / _initialLoadTimeUpdatesPerSecond);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => _loadSettings = e.Parameter as bool?;

        protected override void OnNavigatedFrom(NavigationEventArgs e) => StopTimers();

        private void StopTimers()
        {
            ProgressRing_Loading.Value = 100;
            _initializationDelayTimer.Stop();
            _progressRing_LoadingTimer.Stop();
        }

        private void StartTimers()
        {
            _progressRing_LoadingTimer.Start();
            _initializationDelayTimer.Start();
        }

        private async void InitializationDelayTimer_Tick(object sender, object e)
        {
            StopTimers();
            await _orchestrator.StartOrchestration();
        }

        private void OrchestrationStatusUpdate(string status)
        {
            _statusLog.Add(status);
            TextBlock_Status.Text = status;
        }

        private void OrchestrationCancelled(string reason)
        {
            _statusLog.Add(reason);
            GoToSettings();
        }

        private void GoToSettings()
        {
            StopTimers();
            Frame.Navigate(typeof(Settings), new SettingsPageArguments(_statusLog));
        }

        private void NextAction(Actions.Action action)
        {
            Log.Information("Next Action called: {action}", action.ToString());

            Type nextPage;

            if (_actionToFrameMap.ContainsKey(action.GetType()))
                nextPage = _actionToFrameMap[action.GetType()];
            else
                throw new NotSupportedException($"{Constants.Application.Exceptions.PageDoesNotExist}[{action.GetType().Name}]");

            Frame.Navigate(nextPage, action);
        }

        private void OrchestrationInvalid(List<string> errors)
        {
            Log.Information("OrchestrationInvalid: {errors}", errors);

            foreach (var error in errors)
                _statusLog.Add(error);

            GoToSettings();
        }

        private async void OrchestrationStarted()
        {
            Log.Information("OrchestrationStarted invoked");

            // Start background task to poll for next OrchestrationInstance
            await OrchestrationPollingManager.OrchestrationUpdateTask.RegisterOrchestrationInstanceUpdater();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loadSettings.HasValue && _loadSettings.Value)
                GoToSettings();
            else
                StartTimers();
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Button_Settings_Click Clicked");
            _orchestrator.StopOrchestration(Constants.Application.Main.OpeningSettings);
        }
    }
}
