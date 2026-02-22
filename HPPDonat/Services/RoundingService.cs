namespace HPPDonat.Services;

public sealed class RoundingService : IRoundingService
{
    public decimal RoundUpToHundreds(decimal value)
    {
        if (value <= 0m)
        {
            return 0m;
        }

        return Math.Ceiling(value / 100m) * 100m;
    }
}

