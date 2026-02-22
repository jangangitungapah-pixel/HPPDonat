namespace HPPDonat.Services;

public sealed class ProductionCalculationService : IProductionCalculationService
{
    public decimal HitungProduksiEfektif(decimal jumlahDonatDihasilkan, decimal wastePersen)
    {
        if (jumlahDonatDihasilkan <= 0m)
        {
            return 0m;
        }

        var wasteRatio = Math.Clamp(wastePersen / 100m, 0m, 0.99m);
        return jumlahDonatDihasilkan * (1m - wasteRatio);
    }

    public decimal HitungHppSetelahWaste(decimal totalModalAdonan, decimal produksiEfektif)
    {
        if (totalModalAdonan <= 0m || produksiEfektif <= 0m)
        {
            return 0m;
        }

        return totalModalAdonan / produksiEfektif;
    }
}

