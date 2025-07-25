/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskClient.Pages.PageArguments;
using KioskLibrary.Common;
using KioskLibrary.Pages;
using KioskLibrary.Storage;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KioskClient.Support.Pages
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
        public static void CommonKeyUp(object _, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Home || args.Key == Windows.System.VirtualKey.Escape)
                GoToSettings();
        }

        /// <summary>
        /// Common function used to navigate to the Settings page.
        /// </summary>
        public static void GoToSettings(IApplicationStorage applicationStorage = null)
        {
            (applicationStorage ?? new ApplicationStorage()).SaveToStorage(Constants.ApplicationStorage.EndOrchestration, true);
            var rootFrame = (Application.Current as App)?.Window.Content as Frame;
            rootFrame.Navigate(typeof(Settings), new SettingsPageArguments(new List<string>() { Constants.Application.PageHelper.OrchestrationCancelled }));
        }
    }
}
