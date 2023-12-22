/*
 * Copyright 2023
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Converts a <see cref="ComboBoxItem"/> to a <see cref="string"/>
    /// </summary>
    public class ComboBoxItemsConverter : IValueConverter
    {
        /// <summary>
        /// Convert the value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        /// <summary>
        /// Restore a converted value to its original value
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value as List<ComboBoxItem>;
        }
    }
}
