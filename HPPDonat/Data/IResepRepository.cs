using HPPDonat.Models;

namespace HPPDonat.Data;

public interface IResepRepository
{
    Task<IReadOnlyList<ResepModel>> GetAllAsync();
    Task<ResepModel> AddAsync(ResepModel resep);
    Task UpdateJumlahDipakaiAsync(int id, decimal jumlahDipakai);
    Task DeleteByBahanIdAsync(int bahanId);
}

