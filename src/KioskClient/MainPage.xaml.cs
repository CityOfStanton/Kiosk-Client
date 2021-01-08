using KioskClient.PageArguments;
using KioskClient.Pages;
using KioskClient.Pages.Actions;
using KioskLibrary;
using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KioskClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MainPageArguments currentMainPageArguments = null;
        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp; // Remove any pre-existing Common.CommonKeyUp handlers
            Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp; // Add a single Common.CommonKeyUp handler

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => currentMainPageArguments = e.Parameter as MainPageArguments;

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
                await orchestrator.StartOrchestration();
            }
        }

        private async void RegisterUpdater()
        {
            // Start background task to poll for next OrchestrationInstance
            await OrchestrationPollingManager.OrchestrationUpdateTask.RegisterOrchestrationInstanceUpdater();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await EvaluateMainPageArguments(currentMainPageArguments);
        }
    }
}
