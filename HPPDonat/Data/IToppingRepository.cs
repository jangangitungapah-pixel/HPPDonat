using HPPDonat.Models;

namespace HPPDonat.Data;

public interface IToppingRepository
{
    Task<IReadOnlyList<ToppingModel>> GetAllAsync();
    Task<ToppingModel> AddAsync(ToppingModel topping);
    Task UpdateAsync(ToppingModel topping);
    Task DeleteAsync(int id);
}

