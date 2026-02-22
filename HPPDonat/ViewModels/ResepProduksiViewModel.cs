using HPPDonat.Models;
using HPPDonat.Services;
using System.Collections.ObjectModel;

namespace HPPDonat.ViewModels;

public sealed class ResepProduksiViewModel : ViewModelBase
{
    private readonly IAppStateService _appStateService;

    public ResepProduksiViewModel(IAppStateService appStateService)
    {
        _appStateService = appStateService;
    }

    public ObservableCollection<ResepItemModel> ResepItems => _appStateService.ResepItems;

    public CalculationOutputModel Ringkasan => _appStateService.Calculation;
}

