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
using KioskLibrary.Orchestrations;
using System.Threading.Tasks;

namespace KioskLibrary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Orchestration _orchestration;
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

            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown; // Remove any pre-existing Common.CommonKeyUp handlers
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown; ; // Add a single Common.CommonKeyUp handler

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
            _orchestrator.NextAction += NextAction;
            _orchestrator.OrchestrationCancelled += OrchestrationCancelled;
            _orchestrator.OrchestrationStatusUpdate += OrchestrationStatusUpdate;
            _orchestrator.OrchestrationLoaded += _orchestrator_OrchestrationLoaded;
            OrchestrationStatusUpdate(Constants.Application.Main.AttemptingToLoadDefaultOrchestration);

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            Log.Information("Kiosk Client started");
        }

        private void ProgressRing_LoadingTimer_Tick(object sender, object e)
        {
            ProgressRing_Loading.Value += (100.0 / _initialLoadTimeUpdatesPerSecond);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var startImmediatley = e.Parameter as bool?;
            if (startImmediatley.HasValue && startImmediatley.Value)
                await StartOrchestration();
            else
                StartTimers();
        }

        /// <summary>
        /// Remove the KeyDown binding when we leave
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Home || args.VirtualKey == Windows.System.VirtualKey.Escape)
                CancelOrchestrationFromActionPage();
        }

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
            await StartOrchestration();
        }

        private async Task StartOrchestration()
        {
            StopTimers();
            await _orchestrator.StartOrchestration();
        }

        private void OrchestrationStatusUpdate(string status)
        {
            _statusLog.Add(status);
            TextBlock_Status.Text = status;
        }

        private void _orchestrator_OrchestrationLoaded(Orchestration orchestration)
        {
            _orchestration = orchestration;
        }

        private void OrchestrationCancelled(string reason)
        {
            _statusLog.Add(reason);
            GoToSettings();
        }

        private void GoToSettings()
        {
            StopTimers();
            Frame.Navigate(typeof(Settings), new SettingsPageArguments(_statusLog, _orchestration));
        }

        private void NextAction(Actions.Action action)
        {
            Log.Information("Next Action called: {action}", action.ToString());

            Type nextPage;

            if (_actionToFrameMap.ContainsKey(action.GetType()))
                nextPage = _actionToFrameMap[action.GetType()];
            else
                throw new NotSupportedException($"{Constants.Application.Exceptions.PageDoesNotExist}[{action.GetType().Name}]");

            var apa = new ActionPageArguments(action, CancelOrchestrationFromActionPage);

            Frame.Navigate(nextPage, apa);
        }

        private void CancelOrchestrationFromActionPage()
        {
            _orchestrator.StopOrchestration("User cancelled Orchestration");
        }

        private async void OrchestrationStarted()
        {
            Log.Information("OrchestrationStarted invoked");

            // Start background task to poll for next Orchestration
            await OrchestrationPollingManager.OrchestrationUpdateTask.RegisterOrchestrationUpdater();
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Button_Settings_Click Clicked");
            _orchestrator.StopOrchestration(Constants.Application.Main.OpeningSettings);
        }
    }
}
