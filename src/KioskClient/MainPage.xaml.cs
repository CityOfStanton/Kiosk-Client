using KioskLibrary.PageArguments;
using KioskLibrary.Pages;
using KioskLibrary.Pages.Actions;
using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using KioskClient.Pages;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KioskLibrary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainPageArguments currentPageArguments = null;
        private DispatcherTimer _buttonTimer;

        public MainPage()
        {
            InitializeComponent();

            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp; // Remove any pre-existing Common.CommonKeyUp handlers
            Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp; // Add a single Common.CommonKeyUp handler

            _buttonTimer = new DispatcherTimer();
            _buttonTimer.Interval = TimeSpan.FromSeconds(3);
            _buttonTimer.Tick += _buttonTimer_Tick;
            _buttonTimer.Start();

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        private void _buttonTimer_Tick(object sender, object e)
        {
            Button_Settings.Visibility = Visibility.Visible;
            _buttonTimer.Stop();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => currentPageArguments = e.Parameter as MainPageArguments;

        protected override void OnNavigatedFrom(NavigationEventArgs e) => _buttonTimer.Stop();

        private async Task EvaluateMainPageArguments(MainPageArguments mainPageArguments)
        {
            if (mainPageArguments != null && mainPageArguments is MainPageArguments)
                if (mainPageArguments.ShowSetupInformation)
                    Frame.Navigate(typeof(Settings));
                else
                    return;
            else
            {

                var actionToFrameMap = new Dictionary<Type, Type>();
                actionToFrameMap.Add(typeof(ImageAction), typeof(ImagePage));
                actionToFrameMap.Add(typeof(WebsiteAction), typeof(WebsitePage));

                var orchestrator = new Orchestrator(typeof(Settings), actionToFrameMap, Frame);
                orchestrator.OrchestrationStarted += RegisterUpdater;
                orchestrator.OrchestrationInvalid += OrchestrationInvalid;

                await orchestrator.StartOrchestration();
            }
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await EvaluateMainPageArguments(currentPageArguments);
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }
    }
}
