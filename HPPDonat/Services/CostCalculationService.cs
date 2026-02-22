using HPPDonat.Models;

namespace HPPDonat.Services;

public sealed class CostCalculationService : ICostCalculationService
{
    public decimal HitungModalBahan(decimal jumlahDipakai, decimal nettoPerPack, decimal hargaPerPack)
    {
        if (jumlahDipakai <= 0m || nettoPerPack <= 0m || hargaPerPack <= 0m)
        {
            return 0m;
        }

        return (jumlahDipakai / nettoPerPack) * hargaPerPack;
    }

    public decimal HitungTotalModalAdonan(IEnumerable<ResepItemModel> resepItems)
    {
        return resepItems.Sum(item => HitungModalBahan(item.JumlahDipakai, item.NettoPerPack, item.HargaPerPack));
    }

    public decimal HitungHppDonat(decimal totalModalAdonan, decimal jumlahDonatDihasilkan)
    {
        if (totalModalAdonan <= 0m || jumlahDonatDihasilkan <= 0m)
        {
            return 0m;
        }

        return totalModalAdonan / jumlahDonatDihasilkan;
    }

    public decimal HitungTotalTopping(IEnumerable<ToppingModel> toppingItems)
    {
        return toppingItems.Where(topping => topping.IsActive).Sum(topping => topping.BiayaPerDonat);
    }

    public decimal HitungHppFinal(decimal hppDonat, decimal totalTopping)
    {
        if (hppDonat <= 0m)
        {
            return 0m;
        }

        return hppDonat + totalTopping;
    }
}

