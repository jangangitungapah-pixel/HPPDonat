namespace HPPDonat.Services;

public sealed class ProfitService : IProfitService
{
    private readonly IRoundingService _roundingService;

    public ProfitService(IRoundingService roundingService)
    {
        _roundingService = roundingService;
    }

    public decimal HitungHargaJual(decimal hppFinal, decimal targetProfitPersen)
    {
        if (hppFinal <= 0m)
        {
            return 0m;
        }

        var denominator = 1m - (targetProfitPersen / 100m);
        if (denominator <= 0m)
        {
            return 0m;
        }

        var hargaJualRaw = hppFinal / denominator;
        return _roundingService.RoundUpToHundreds(hargaJualRaw);
    }

    public decimal HitungProfitPerDonat(decimal hargaJual, decimal hppFinal)
    {
        return hargaJual - hppFinal;
    }

    public decimal HitungTotalProfit(decimal profitPerDonat, decimal produksiEfektif)
    {
        if (profitPerDonat <= 0m || produksiEfektif <= 0m)
        {
            return 0m;
        }

        return profitPerDonat * produksiEfektif;
    }

    public decimal HitungEstimasiBulanan(decimal totalProfitBatch, int hariProduksiPerBulan)
    {
        if (totalProfitBatch <= 0m || hariProduksiPerBulan <= 0)
        {
            return 0m;
        }

        return totalProfitBatch * hariProduksiPerBulan;
    }
}

