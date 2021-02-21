/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskClient.Pages.PageArguments;
using KioskLibrary.Actions;
using KioskLibrary.Helpers;
using KioskLibrary.ViewModels;
using Serilog;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace KioskLibrary.Pages.Actions
{
    /// <summary>
    /// A page for displaying an image
    /// </summary>
    public sealed partial class ImagePage : Page
    {
        private ActionViewModel State { get; set; } // Variable name is not in _ format because it is being referenced in associated partial class
        private System.Action _cancelOrchestration;

        /// <summary>
        /// Constructor
        /// </summary>
        public ImagePage()
        {
            InitializeComponent();

            if (State == null)
                State = new ActionViewModel();
        }

        /// <summary>
        /// Class that's called when this page has been navigated to.
        /// </summary>
        /// <param name="e">Navigation event args</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {

                var apa = e.Parameter as ActionPageArguments;
                var action = apa.Action as ImageAction;
                _cancelOrchestration = apa.CancelOrchestration;

                Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown; // Remove any pre-existing Common.CommonKeyUp handlers
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown; ; // Add a single Common.CommonKeyUp handler

                Log.Information("ImagePage OnNavigatedTo: {data}", SerializationHelper.JSONSerialize(action));

                var validationResult = await action.ValidateAsync();

                State.IsContentSourceValid = validationResult.IsValid;
                State.FailedToLoadContentMessageDetail = validationResult.GetValidationSummaryOfChildren();

                if (State.IsContentSourceValid.Value)
                {
                    Image_Display.Source = new BitmapImage(new Uri(action.Path));
                    Image_Display.Stretch = action.Stretch;
                }
                else
                    Log.Error("Failed to validate {action} due to the following errors: {errors}", action, validationResult);
            }
            catch (Exception ex)
            {
                State.IsContentSourceValid = false;
                State.FailedToLoadContentMessageDetail = ex.Message;
                Log.Error(ex, ex.Message);
            }
        }

        /// <summary>
        /// Remove the KeyDown binding when we leave
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Home || args.VirtualKey == Windows.System.VirtualKey.Escape)
                _cancelOrchestration();
        }
    }
}
