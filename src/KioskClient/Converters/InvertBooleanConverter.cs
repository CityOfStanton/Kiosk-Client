/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using Windows.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => !(bool)value;

        public object ConvertBack(object value, Type targetType, object parameter, string language) => !(bool)value;
    }
}
