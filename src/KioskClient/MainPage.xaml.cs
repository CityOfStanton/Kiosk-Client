using KioskLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MainPageArguments)
                await EvaluateMainPageArguments(e.Parameter as MainPageArguments);
        }

        private async Task EvaluateMainPageArguments(MainPageArguments mainPageArguments)
        {
            if (mainPageArguments != null)
                if (mainPageArguments.ShowSetupInformation)
                    return; // Show the Setup Information dialog

            var orchestration = await Common.GetSettingsFromServer();
            if (orchestration != null)
                Common.LoadNextAction(orchestration, null, this.Frame);
            else
                this.Frame.Navigate(typeof(Settings));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await EvaluateMainPageArguments(null);
        }
    }
}
