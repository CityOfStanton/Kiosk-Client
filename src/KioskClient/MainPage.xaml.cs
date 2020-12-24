using KioskLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

            Window.Current.CoreWindow.KeyDown -= Common.CommonKeyUp; // Remove any pre-existing Common.CommonKeyUp handlers
            Window.Current.CoreWindow.KeyDown += Common.CommonKeyUp; // Add a single Common.CommonKeyUp handler

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => currentMainPageArguments = e.Parameter as MainPageArguments;

        private async Task EvaluateMainPageArguments(MainPageArguments mainPageArguments)
        {
            if (mainPageArguments != null && mainPageArguments is MainPageArguments)
                if (mainPageArguments.ShowSetupInformation)
                    this.Frame.Navigate(typeof(Settings));
                else
                    return;
            else
                await Common.LoadOrchestration();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await EvaluateMainPageArguments(currentMainPageArguments);
        }
    }
}
