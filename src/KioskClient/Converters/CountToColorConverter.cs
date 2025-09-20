/*
 * Copyright 2025
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Converts a number greater than 0 to the <see cref="Brush"/>
    /// </summary>
    public class CountToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a number greater than 0 to the <see cref="Brush"/>
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var defaultBrush = (SolidColorBrush)Application.Current.Resources["DefaultTextForegroundThemeBrush"];
            var color = (Color)XamlBindingHelper.ConvertValue(typeof(Color), parameter);
            var brush = new SolidColorBrush(color);

            return System.Convert.ToInt32(value) > 0 ? brush : defaultBrush;
        }

        /// <summary>
        /// Restores a converted int value to a <see cref="Brush"/>
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
