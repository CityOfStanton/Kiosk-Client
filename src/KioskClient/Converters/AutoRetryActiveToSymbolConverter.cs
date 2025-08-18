/*
 * Copyright 2025
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    public class AutoRetryActiveToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isActive = (bool)value;
            return isActive ? Symbol.Stop : Symbol.Play;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
