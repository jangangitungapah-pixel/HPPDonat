using HPPDonat.Models;
using HPPDonat.Services;

namespace HPPDonat.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    public override string Title => "Dashboard";

    public DashboardViewModel(IAppStateService appStateService)
    {
        State = appStateService;
    }

    public string CurrentDate => DateTime.Now.ToString("dddd, dd MMMM yyyy");

    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            return hour < 12 ? "Selamat Pagi" : hour < 18 ? "Selamat Siang" : "Selamat Malam";
        }
    }

    public IAppStateService State { get; }

    public CalculationOutputModel Ringkasan => State.Calculation;

    public ProduksiSettingModel ProduksiSetting => State.ProduksiSetting;
}

