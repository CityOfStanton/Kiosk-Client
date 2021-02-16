/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Actions;
using KioskLibrary.Helpers;
using KioskLibrary.ViewModels;
using Serilog;
using System;
using System.Linq;
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
        private ImageAction _action;

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
                _action = e.Parameter as ImageAction;

                Log.Information("ImagePage OnNavigatedTo: {data}", SerializationHelper.JSONSerialize(_action));

                (var validationResult, _, var errors) = await _action.ValidateAsync();

                State.IsContentSourceValid = validationResult;
                State.FailedToLoadContentMessageDetail = errors.FirstOrDefault();

                if (State.IsContentSourceValid.Value)
                {
                    Image_Display.Source = new BitmapImage(new Uri(_action.Path));
                    Image_Display.Stretch = _action.Stretch;
                }
                else
                    Log.Error("Failed to validate {action} due to the following errors: {errors}", _action, errors);
            }
            catch (Exception ex)
            {
                State.IsContentSourceValid = false;
                State.FailedToLoadContentMessageDetail = ex.Message;
                Log.Error(ex, ex.Message);
            }
        }
    }
}
