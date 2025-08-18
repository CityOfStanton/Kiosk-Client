/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Hides <see cref="OrchestrationSource.File"/>
    /// </summary>
    /// <remarks><see cref="null"/> is converted to <see cref="Visibility.Collapsed"/>, anything else is converted to <see cref="Visibility.Visible"/></remarks>
    public class HideFileOrchestrationSourceConverter : IValueConverter
    {
        /// <summary>
        /// Returns <see cref="Visibility.Collapsed"/> if the value is <see cref="OrchestrationSource.File"/>
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (OrchestrationSource)value == OrchestrationSource.File ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
