/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Actions;
using KioskLibrary.Helpers;
using Serilog;
using System;
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
        /// <summary>
        /// Constructor
        /// </summary>
        public ImagePage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Class that's called when this page has been navigated to.
        /// </summary>
        /// <param name="e">Navigation event args</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var action = e.Parameter as ImageAction;

            Log.Information("ImagePage OnNavigatedTo: {data}", SerializationHelper.JSONSerialize(action));

            imgDisplay.Source = new BitmapImage(new Uri(action.Path));
            imgDisplay.Stretch = action.Stretch;
        }
    }
}
