using HPPDonat.Services;
using ReactiveUI;
using System.Reactive;

namespace HPPDonat.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly MasterBahanViewModel _masterBahanViewModel;
    private readonly ResepProduksiViewModel _resepProduksiViewModel;
    private readonly ToppingViewModel _toppingViewModel;
    private readonly KalkulatorProduksiViewModel _kalkulatorProduksiViewModel;
    private readonly AnalisaProfitViewModel _analisaProfitViewModel;
    private readonly IThemeService _themeService;

    private object _currentPage;
    private bool _isDarkMode;
    private string _judulHalaman = "Dashboard";

    public MainWindowViewModel(
        DashboardViewModel dashboardViewModel,
        MasterBahanViewModel masterBahanViewModel,
        ResepProduksiViewModel resepProduksiViewModel,
        ToppingViewModel toppingViewModel,
        KalkulatorProduksiViewModel kalkulatorProduksiViewModel,
        AnalisaProfitViewModel analisaProfitViewModel,
        IThemeService themeService)
    {
        _dashboardViewModel = dashboardViewModel;
        _masterBahanViewModel = masterBahanViewModel;
        _resepProduksiViewModel = resepProduksiViewModel;
        _toppingViewModel = toppingViewModel;
        _kalkulatorProduksiViewModel = kalkulatorProduksiViewModel;
        _analisaProfitViewModel = analisaProfitViewModel;
        _themeService = themeService;

        _currentPage = _dashboardViewModel;

        NavigateCommand = ReactiveCommand.Create<string>(Navigasi);
    }

    public string NamaAplikasi => "HPP Donat Calculator";

    public object CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public string JudulHalaman
    {
        get => _judulHalaman;
        private set => this.RaiseAndSetIfChanged(ref _judulHalaman, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isDarkMode, value);
            _themeService.ApplyTheme(value);
        }
    }

    public ReactiveCommand<string, Unit> NavigateCommand { get; }

    private void Navigasi(string halaman)
    {
        switch (halaman)
        {
            case "Dashboard":
                CurrentPage = _dashboardViewModel;
                JudulHalaman = "Dashboard";
                break;
            case "MasterBahan":
                CurrentPage = _masterBahanViewModel;
                JudulHalaman = "Master Bahan";
                break;
            case "Resep":
                CurrentPage = _resepProduksiViewModel;
                JudulHalaman = "Resep Produksi";
                break;
            case "Topping":
                CurrentPage = _toppingViewModel;
                JudulHalaman = "Manajemen Topping";
                break;
            case "Kalkulator":
                CurrentPage = _kalkulatorProduksiViewModel;
                JudulHalaman = "Kalkulator Produksi";
                break;
            case "Analisa":
                CurrentPage = _analisaProfitViewModel;
                JudulHalaman = "Analisa Profit";
                break;
        }
    }
}

