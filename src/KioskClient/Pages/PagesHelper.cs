﻿/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary;
using KioskLibrary.Common;
using KioskLibrary.PageArguments;
using KioskLibrary.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace KioskClient.Pages
{
    /// <summary>
    /// Helper class for common page-level functionality
    /// </summary>
    public class PagesHelper
    {
        /// <summary>
        /// Common function to call when the KeyUp event has been fired.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        public static void CommonKeyUp(object sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Home || args.VirtualKey == Windows.System.VirtualKey.Escape)
                GoToSettings();
        }

        /// <summary>
        /// Common function to call when the KeyUp event has been fired.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        public static void CommonKeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Home || args.Key == Windows.System.VirtualKey.Escape)
                GoToSettings();
        }

        /// <summary>
        /// Common function used to navigate to the Settings page.
        /// </summary>
        public static void GoToSettings()
        {
            ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.EndOrchestration, true);
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), new MainPageArguments(showSetupInformation: true));
        }
    }
}
