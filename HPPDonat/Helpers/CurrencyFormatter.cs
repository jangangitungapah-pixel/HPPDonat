using System.Globalization;

namespace HPPDonat.Helpers;

public static class CurrencyFormatter
{
    private static readonly CultureInfo IndonesianCulture = CultureInfo.GetCultureInfo("id-ID");

    public static string ToRupiah(decimal value)
    {
        return string.Format(IndonesianCulture, "{0:C0}", value);
    }
}

