using System.ComponentModel;
using HPPDonat.Services;
using ReactiveUI;

namespace HPPDonat.ViewModels;

public sealed class AnalisaProfitViewModel : ViewModelBase
{
    private readonly IAppStateService _appStateService;
    private readonly IProductionCalculationService _productionCalculationService;
    private readonly IProfitService _profitService;

    private decimal _simulasiTargetProfitPersen;
    private decimal _simulasiWastePersen;
    private decimal _simulasiProduksiEfektif;
    private decimal _simulasiHargaJual;
    private decimal _simulasiProfitPerDonat;
    private decimal _simulasiTotalProfit;
    private decimal _simulasiProfitBulanan;

    public AnalisaProfitViewModel(
        IAppStateService appStateService,
        IProductionCalculationService productionCalculationService,
        IProfitService profitService)
    {
        _appStateService = appStateService;
        _productionCalculationService = productionCalculationService;
        _profitService = profitService;

        _simulasiTargetProfitPersen = _appStateService.ProduksiSetting.TargetProfitPersen;
        _simulasiWastePersen = _appStateService.ProduksiSetting.WastePersen;

        _appStateService.Calculation.PropertyChanged += OnAppStateChanged;
        _appStateService.ProduksiSetting.PropertyChanged += OnAppStateChanged;

        this.WhenAnyValue(
                viewModel => viewModel.SimulasiTargetProfitPersen,
                viewModel => viewModel.SimulasiWastePersen)
            .Subscribe(_ => HitungSimulasi());

        HitungSimulasi();
    }

    public decimal SimulasiTargetProfitPersen
    {
        get => _simulasiTargetProfitPersen;
        set => this.RaiseAndSetIfChanged(ref _simulasiTargetProfitPersen, value);
    }

    public decimal SimulasiWastePersen
    {
        get => _simulasiWastePersen;
        set => this.RaiseAndSetIfChanged(ref _simulasiWastePersen, value);
    }

    public decimal SimulasiProduksiEfektif
    {
        get => _simulasiProduksiEfektif;
        private set => this.RaiseAndSetIfChanged(ref _simulasiProduksiEfektif, value);
    }

    public decimal SimulasiHargaJual
    {
        get => _simulasiHargaJual;
        private set => this.RaiseAndSetIfChanged(ref _simulasiHargaJual, value);
    }

    public decimal SimulasiProfitPerDonat
    {
        get => _simulasiProfitPerDonat;
        private set => this.RaiseAndSetIfChanged(ref _simulasiProfitPerDonat, value);
    }

    public decimal SimulasiTotalProfit
    {
        get => _simulasiTotalProfit;
        private set => this.RaiseAndSetIfChanged(ref _simulasiTotalProfit, value);
    }

    public decimal SimulasiProfitBulanan
    {
        get => _simulasiProfitBulanan;
        private set => this.RaiseAndSetIfChanged(ref _simulasiProfitBulanan, value);
    }

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        HitungSimulasi();
    }

    private void HitungSimulasi()
    {
        var produksiEfektif = _productionCalculationService.HitungProduksiEfektif(
            _appStateService.ProduksiSetting.JumlahDonatDihasilkan,
            SimulasiWastePersen);
        var hargaJual = _profitService.HitungHargaJual(
            _appStateService.Calculation.HppFinal,
            SimulasiTargetProfitPersen);
        var profitPerDonat = _profitService.HitungProfitPerDonat(hargaJual, _appStateService.Calculation.HppFinal);
        var totalProfit = _profitService.HitungTotalProfit(profitPerDonat, produksiEfektif);
        var profitBulanan = _profitService.HitungEstimasiBulanan(
            totalProfit,
            _appStateService.ProduksiSetting.HariProduksiPerBulan);

        SimulasiProduksiEfektif = produksiEfektif;
        SimulasiHargaJual = hargaJual;
        SimulasiProfitPerDonat = profitPerDonat;
        SimulasiTotalProfit = totalProfit;
        SimulasiProfitBulanan = profitBulanan;
    }
}

