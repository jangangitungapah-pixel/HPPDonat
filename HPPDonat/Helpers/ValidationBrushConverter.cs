using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HPPDonat.Helpers;

public sealed class ValidationBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && string.Equals(text, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return Brushes.ForestGreen;
        }

        return Brushes.OrangeRed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
