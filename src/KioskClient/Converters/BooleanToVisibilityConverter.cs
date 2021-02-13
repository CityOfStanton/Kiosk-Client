/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Converts a boolean value to a <see cref="Visibility"/>
    /// </summary>
    /// <remarks><see cref="true"/> is converted to <see cref="Visibility.Visible"/> and <see cref="false"/> is converted to <see cref="Visibility.Collapsed"/></remarks>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert the boolean value to a <see cref="Visibility"/>
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language) => (bool)value ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Restores a converted boolean value to a <see cref="Visibility"/>
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => (Visibility)value == Visibility.Visible;
    }
}
