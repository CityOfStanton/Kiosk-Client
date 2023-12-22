/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Windows.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Converts a <see cref="ValidationResultType"/> to a <see cref="Brush"/>
    /// </summary>
    public class BooleanToColorConverter : IValueConverter
    {
        /// <summary>
        /// Convert the value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? new SolidColorBrush(Color.FromArgb(255, 38, 123, 48)) : new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        }

        /// <summary>
        /// Restore a converted value to its original value
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
