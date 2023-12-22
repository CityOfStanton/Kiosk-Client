/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Converts a <see cref="ValidationResultType"/> to a <see cref="string"/> used in a <see cref="FontIcon"/>
    /// </summary>
    public class BooleanToSymbolConverter : IValueConverter
    {
        /// <summary>
        /// Convert the value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? "\xF78C" : "\xE711";
        }

        /// <summary>
        /// Restore a converted value to its original value
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
