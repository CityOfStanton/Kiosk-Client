/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Inverts a boolean value , then converts it to a <see cref="Visibility"/>
    /// </summary>
    /// <remarks><see cref="false"/> is converted to <see cref="Visibility.Visible"/> and <see cref="true"/> is converted to <see cref="Visibility.Collapsed"/></remarks>
    public class BooleanToInvertedVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert the boolean value to a <see cref="Visibility"/>
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool?)
                if ((value as bool?).HasValue)
                    return (value as bool?).Value ? Visibility.Collapsed: Visibility.Visible;
                else
                    return Visibility.Visible;
            else
                return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Restores a converted boolean value to a <see cref="Visibility"/>
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => (Visibility)value != Visibility.Visible;
    }
}
