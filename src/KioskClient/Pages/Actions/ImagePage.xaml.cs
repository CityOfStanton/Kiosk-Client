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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

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
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // No equivalent event handling for WinUI 3, removing KeyDown logic
        }
    }
}
