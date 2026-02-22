namespace HPPDonat.Services;

public interface IProductionCalculationService
{
    decimal HitungProduksiEfektif(decimal jumlahDonatDihasilkan, decimal wastePersen);
    decimal HitungHppSetelahWaste(decimal totalModalAdonan, decimal produksiEfektif);
}

