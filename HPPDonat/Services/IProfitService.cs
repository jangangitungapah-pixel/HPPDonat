namespace HPPDonat.Services;

public interface IProfitService
{
    decimal HitungHargaJual(decimal hppFinal, decimal targetProfitPersen);
    decimal HitungProfitPerDonat(decimal hargaJual, decimal hppFinal);
    decimal HitungTotalProfit(decimal profitPerDonat, decimal produksiEfektif);
    decimal HitungEstimasiBulanan(decimal totalProfitBatch, int hariProduksiPerBulan);
}

