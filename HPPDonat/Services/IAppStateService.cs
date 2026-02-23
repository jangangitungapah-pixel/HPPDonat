using System.Collections.ObjectModel;
using System.ComponentModel;
using HPPDonat.Models;

namespace HPPDonat.Services;

public interface IAppStateService : INotifyPropertyChanged
{
    ObservableCollection<BahanModel> BahanItems { get; }
    ObservableCollection<ResepItemModel> ResepItems { get; }
    ObservableCollection<ResepVarianModel> ResepVarianItems { get; }
    ObservableCollection<ToppingModel> ToppingItems { get; }
    ProduksiSettingModel ProduksiSetting { get; }
    CalculationOutputModel Calculation { get; }
    AppStatusModel Status { get; }
    ResepVarianModel? VarianAktif { get; }

    Task InitializeAsync();
    Task TambahBahanAsync();
    Task HapusBahanAsync(BahanModel? bahan);
    Task TambahToppingAsync();
    Task HapusToppingAsync(ToppingModel? topping);
    Task PilihVarianResepAsync(ResepVarianModel? varian);
    Task TambahVarianResepAsync(string? namaVarian = null, bool duplikatDariVarianAktif = false);
    Task DuplikasiVarianAktifAsync();
    Task HapusVarianResepAsync(ResepVarianModel? varian);
    Task ResetResepAktifAsync();
    void ClearErrorStatus();
}