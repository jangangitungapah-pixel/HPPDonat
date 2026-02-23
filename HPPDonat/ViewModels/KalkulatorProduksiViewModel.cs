using HPPDonat.Models;
using HPPDonat.Services;

namespace HPPDonat.ViewModels;

public sealed class KalkulatorProduksiViewModel : ViewModelBase
{
    public override string Title => "Kalkulator Produksi";

    private readonly IAppStateService _appStateService;

    public KalkulatorProduksiViewModel(IAppStateService appStateService)
    {
        _appStateService = appStateService;
    }

    public ProduksiSettingModel ProduksiSetting => _appStateService.ProduksiSetting;

    public CalculationOutputModel Ringkasan => _appStateService.Calculation;
}

