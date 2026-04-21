using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace KioskClient.Converters;

/// <summary>Converts a boolean to Visibility (true = Visible, false = Collapsed).</summary>
public partial class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}

/// <summary>Converts a boolean to inverted Visibility (true = Collapsed, false = Visible).</summary>
public partial class BooleanToInvertedVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Collapsed;
}

/// <summary>Inverts a boolean value.</summary>
public partial class InvertBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is bool b ? !b : value;
}

/// <summary>Converts null to Collapsed, non-null to Visible.</summary>
public partial class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Converts a nullable boolean to a validation symbol (checkmark or X).</summary>
public partial class BooleanToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? "\xF78C" : "\xE711";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Converts a boolean to a color brush (green for true, red for false).</summary>
public partial class BooleanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = value is true
            ? Windows.UI.Color.FromArgb(255, 38, 123, 48)   // #267B30
            : Windows.UI.Color.FromArgb(255, 220, 50, 50);  // Red
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Converts a boolean to Play/Stop symbol for auto-retry button.</summary>
public partial class AutoRetryActiveToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Microsoft.UI.Xaml.Controls.Symbol.Stop : Microsoft.UI.Xaml.Controls.Symbol.Play;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
