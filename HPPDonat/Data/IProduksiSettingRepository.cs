using HPPDonat.Models;

namespace HPPDonat.Data;

public interface IProduksiSettingRepository
{
    Task<ProduksiSettingModel> GetOrCreateAsync();
    Task UpdateAsync(ProduksiSettingModel setting);
}

