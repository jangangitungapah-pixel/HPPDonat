using HPPDonat.Models;

namespace HPPDonat.Services;

public interface ICostCalculationService
{
    decimal HitungModalBahan(decimal jumlahDipakai, decimal nettoPerPack, decimal hargaPerPack);
    decimal HitungTotalModalAdonan(IEnumerable<ResepItemModel> resepItems);
    decimal HitungHppDonat(decimal totalModalAdonan, decimal jumlahDonatDihasilkan);
    decimal HitungTotalTopping(IEnumerable<ToppingModel> toppingItems);
    decimal HitungHppFinal(decimal hppDonat, decimal totalTopping);
}

