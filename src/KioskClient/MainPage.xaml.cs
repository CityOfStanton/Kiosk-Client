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
        private readonly DispatcherTimer _loadCompletionTime;
        private readonly Orchestrator _orchestrator;
        private readonly Dictionary<Type, Type> _actionToFrameMap;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

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

            _loadCompletionTime = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _loadCompletionTime.Tick += LoadCompletionTime_Tick;

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

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            Log.Information("Kiosk Client started");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => _loadSettings = e.Parameter as bool?;

        protected override void OnNavigatedFrom(NavigationEventArgs e) => _loadCompletionTime.Stop();

        private async void LoadCompletionTime_Tick(object sender, object e)
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

            Frame.Navigate(typeof(Settings), new SettingsPageArguments(errors));
        }

        private async void OrchestrationStarted()
        {
            Log.Information("OrchestrationStarted invoked");

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
            Log.Information("Button_Settings_Click Clicked");

            _loadCompletionTime.Stop();
            Frame.Navigate(typeof(Settings));
        }
    }
}
