using ReactiveUI;

namespace HPPDonat.Models;

public sealed class CalculationOutputModel : ReactiveObject
{
    private decimal _totalModalAdonan;
    private decimal _hppDonat;
    private decimal _totalTopping;
    private decimal _hppFinal;
    private decimal _produksiEfektif;
    private decimal _hppSetelahWaste;
    private decimal _hargaJual;
    private decimal _profitPerDonat;
    private decimal _totalProfit;
    private decimal _estimasiHarian;
    private decimal _estimasiBulanan;

    public decimal TotalModalAdonan
    {
        get => _totalModalAdonan;
        set => this.RaiseAndSetIfChanged(ref _totalModalAdonan, value);
    }

    public decimal HppDonat
    {
        get => _hppDonat;
        set => this.RaiseAndSetIfChanged(ref _hppDonat, value);
    }

    public decimal TotalTopping
    {
        get => _totalTopping;
        set => this.RaiseAndSetIfChanged(ref _totalTopping, value);
    }

    public decimal HppFinal
    {
        get => _hppFinal;
        set => this.RaiseAndSetIfChanged(ref _hppFinal, value);
    }

    public decimal ProduksiEfektif
    {
        get => _produksiEfektif;
        set => this.RaiseAndSetIfChanged(ref _produksiEfektif, value);
    }

    public decimal HppSetelahWaste
    {
        get => _hppSetelahWaste;
        set => this.RaiseAndSetIfChanged(ref _hppSetelahWaste, value);
    }

    public decimal HargaJual
    {
        get => _hargaJual;
        set => this.RaiseAndSetIfChanged(ref _hargaJual, value);
    }

    public decimal ProfitPerDonat
    {
        get => _profitPerDonat;
        set => this.RaiseAndSetIfChanged(ref _profitPerDonat, value);
    }

    public decimal TotalProfit
    {
        get => _totalProfit;
        set => this.RaiseAndSetIfChanged(ref _totalProfit, value);
    }

    public decimal EstimasiHarian
    {
        get => _estimasiHarian;
        set => this.RaiseAndSetIfChanged(ref _estimasiHarian, value);
    }

    public decimal EstimasiBulanan
    {
        get => _estimasiBulanan;
        set => this.RaiseAndSetIfChanged(ref _estimasiBulanan, value);
    }
}

