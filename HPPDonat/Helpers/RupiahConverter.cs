using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HPPDonat.Helpers;

public sealed class RupiahConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return CurrencyFormatter.ToRupiah(decimalValue);
        }

        if (value is double doubleValue)
        {
            return CurrencyFormatter.ToRupiah((decimal)doubleValue);
        }

        if (value is int intValue)
        {
            return CurrencyFormatter.ToRupiah(intValue);
        }

        return CurrencyFormatter.ToRupiah(0m);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}

