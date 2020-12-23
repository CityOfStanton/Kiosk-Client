using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KioskClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();

            var settingsUri = Common.GetSettingsUri();
            tbSettingsUri.Text = settingsUri?.AbsoluteUri ?? "";
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveSettingsUri(tbSettingsUri.Text);
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            await Common.GetSettingsFromServer();
        }
    }
}
