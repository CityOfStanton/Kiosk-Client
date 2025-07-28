/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Microsoft.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Inverts the boolean value for data bound elements
    /// </summary>
    public class InvertBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Invert the value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language) => ConvertValue(value);

        /// <summary>
        /// Restore a converted value to its original value
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => ConvertValue(value);

        private static object ConvertValue(object value)
        {
            if (value is bool?)
                if ((value as bool?).HasValue)
                    return !(value as bool?).Value;
                else
                    return null;
            else
                return !(bool)value;
        }
    }
}
