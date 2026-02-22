using HPPDonat.Models;

namespace HPPDonat.Data;

public interface IBahanRepository
{
    Task<IReadOnlyList<BahanModel>> GetAllAsync();
    Task<BahanModel> AddAsync(BahanModel bahan);
    Task UpdateAsync(BahanModel bahan);
    Task DeleteAsync(int id);
}

