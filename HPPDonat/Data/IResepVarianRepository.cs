using HPPDonat.Models;

namespace HPPDonat.Data;

public interface IResepVarianRepository
{
    Task<IReadOnlyList<ResepVarianModel>> GetAllAsync();
    Task<ResepVarianModel> AddAsync(string namaVarian, bool isActive = false);
    Task SetActiveAsync(int id);
    Task DeleteAsync(int id);
}
