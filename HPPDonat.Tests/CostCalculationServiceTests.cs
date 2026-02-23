using HPPDonat.Models;
using HPPDonat.Services;

namespace HPPDonat.Tests;

public sealed class CostCalculationServiceTests
{
    private readonly CostCalculationService _service = new();

    [Fact]
    public void HitungModalBahan_MengikutiRumus()
    {
        var modal = _service.HitungModalBahan(500m, 1000m, 20000m);
        Assert.Equal(10000m, modal);
    }

    [Fact]
    public void HitungTotalModalAdonan_MenjumlahkanSemuaModal()
    {
        var items = new[]
        {
            new ResepItemModel { JumlahDipakai = 500m, NettoPerPack = 1000m, HargaPerPack = 20000m },
            new ResepItemModel { JumlahDipakai = 250m, NettoPerPack = 1000m, HargaPerPack = 12000m }
        };

        var total = _service.HitungTotalModalAdonan(items);

        Assert.Equal(13000m, total);
    }

    [Fact]
    public void HitungTotalTopping_HanyaYangAktif()
    {
        var toppings = new[]
        {
            new ToppingModel { BiayaPerDonat = 400m, IsActive = true },
            new ToppingModel { BiayaPerDonat = 600m, IsActive = false },
            new ToppingModel { BiayaPerDonat = 150m, IsActive = true }
        };

        var total = _service.HitungTotalTopping(toppings);

        Assert.Equal(550m, total);
    }

    [Fact]
    public void HitungHppFinal_MenjumlahkanHppDanTopping()
    {
        var hppFinal = _service.HitungHppFinal(2450m, 550m);
        Assert.Equal(3000m, hppFinal);
    }
}
