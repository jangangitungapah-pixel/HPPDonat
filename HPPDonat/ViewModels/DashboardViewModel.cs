using HPPDonat.Models;
using HPPDonat.Services;

namespace HPPDonat.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel(IAppStateService appStateService)
    {
        State = appStateService;
    }

    public IAppStateService State { get; }

    public CalculationOutputModel Ringkasan => State.Calculation;

    public ProduksiSettingModel ProduksiSetting => State.ProduksiSetting;
}

