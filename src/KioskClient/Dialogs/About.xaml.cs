/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Windows.ApplicationModel;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;

namespace KioskClient.Dialogs
{
    public sealed partial class About : ContentDialog
    {
        public string Version { get; set; }

        public About()
        {
            Version = GetAppVersion();

            this.InitializeComponent();
        }

        private static string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private void ContentDialog_About_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
                Hide();
        }

        private void Button_Close_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Hide();
        }

        private async void Button_Logs_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var logFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = await StorageFolder.GetFolderFromPathAsync(logFolderPath);
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
    }
}

