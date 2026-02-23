using HPPDonat.Services;

namespace HPPDonat.Tests;

public sealed class ProductionAndProfitServiceTests
{
    private readonly ProductionCalculationService _productionService = new();
    private readonly ProfitService _profitService = new(new RoundingService());

    [Fact]
    public void HitungProduksiEfektif_SesuaiWaste()
    {
        var produksiEfektif = _productionService.HitungProduksiEfektif(100m, 10m);
        Assert.Equal(90m, produksiEfektif);
    }

    [Fact]
    public void HitungHppSetelahWaste_SesuaiRumus()
    {
        var hppWaste = _productionService.HitungHppSetelahWaste(180000m, 90m);
        Assert.Equal(2000m, hppWaste);
    }

    [Fact]
    public void HitungHargaJual_DibulatkanKeAtasKelipatanSeratus()
    {
        var hargaJual = _profitService.HitungHargaJual(2340m, 25m);
        Assert.Equal(3200m, hargaJual);
    }

    [Fact]
    public void HitungProfitBatch_SesuaiRumus()
    {
        var hargaJual = _profitService.HitungHargaJual(2000m, 20m);
        var profitPerDonat = _profitService.HitungProfitPerDonat(hargaJual, 2000m);
        var totalProfit = _profitService.HitungTotalProfit(profitPerDonat, 100m);

        Assert.Equal(500m, profitPerDonat);
        Assert.Equal(50000m, totalProfit);
    }

    [Fact]
    public void HitungEstimasiBulanan_MengalikanHariProduksi()
    {
        var bulanan = _profitService.HitungEstimasiBulanan(50000m, 26);
        Assert.Equal(1300000m, bulanan);
    }
}
