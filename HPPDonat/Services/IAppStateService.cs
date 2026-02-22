using System.Collections.ObjectModel;
using HPPDonat.Models;

namespace HPPDonat.Services;

public interface IAppStateService
{
    ObservableCollection<BahanModel> BahanItems { get; }
    ObservableCollection<ResepItemModel> ResepItems { get; }
    ObservableCollection<ToppingModel> ToppingItems { get; }
    ProduksiSettingModel ProduksiSetting { get; }
    CalculationOutputModel Calculation { get; }

    Task InitializeAsync();
    Task TambahBahanAsync();
    Task HapusBahanAsync(BahanModel? bahan);
    Task TambahToppingAsync();
    Task HapusToppingAsync(ToppingModel? topping);
}

