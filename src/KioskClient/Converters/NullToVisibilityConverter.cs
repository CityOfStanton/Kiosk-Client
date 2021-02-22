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
    /// Converts a null value to a <see cref="Visibility"/>
    /// </summary>
    /// <remarks><see cref="null"/> is converted to <see cref="Visibility.Collapsed"/>, anything else is converted to <see cref="Visibility.Visible"/></remarks>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert the value to a <see cref="Visibility"/> based on if it's null
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? Visibility.Collapsed: Visibility.Visible;
        }

        /// <summary>
        /// Restores a converted null value to a <see cref="Visibility"/>
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
