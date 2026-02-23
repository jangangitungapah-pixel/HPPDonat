namespace HPPDonat.Helpers;

public static class InputValidator
{
    private static readonly HashSet<string> ValidSatuan = new(StringComparer.OrdinalIgnoreCase)
    {
        "gram", "kg", "ml", "liter", "butir", "pcs", "sendok"
    };

    public static string NormalizeName(string? value, string fallback)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    public static string NormalizeSatuan(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "gram";
        }

        return ValidSatuan.Contains(normalized) ? normalized : "gram";
    }

    public static decimal ClampNonNegative(decimal value)
    {
        return value < 0m ? 0m : value;
    }

    public static decimal ClampPositive(decimal value, decimal fallback = 1m)
    {
        return value <= 0m ? fallback : value;
    }

    public static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return Math.Clamp(value, min, max);
    }

    public static int Clamp(int value, int min, int max)
    {
        return Math.Clamp(value, min, max);
    }
}
