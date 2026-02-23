using HPPDonat.Models;

namespace HPPDonat.Data;

public interface IProductionDataCoordinator
{
    Task<(BahanModel bahan, IReadOnlyList<ResepModel> resepItems)> AddBahanDanResepAsync(BahanModel bahan, IReadOnlyList<int> varianIds, decimal jumlahDipakai);
    Task DeleteBahanDanResepAsync(int bahanId);
}
