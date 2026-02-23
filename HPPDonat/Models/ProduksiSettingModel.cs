using ReactiveUI;

namespace HPPDonat.Models;

public sealed class ProduksiSettingModel : ReactiveObject
{
    private int _id = 1;
    private decimal _jumlahDonatDihasilkan = 100m;
    private decimal _beratPerDonat = 50m;
    private decimal _wastePersen;
    private decimal _targetProfitPersen = 30m;
    private int _hariProduksiPerBulan = 26;

    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public decimal JumlahDonatDihasilkan
    {
        get => _jumlahDonatDihasilkan;
        set => this.RaiseAndSetIfChanged(ref _jumlahDonatDihasilkan, value);
    }

    public decimal BeratPerDonat
    {
        get => _beratPerDonat;
        set => this.RaiseAndSetIfChanged(ref _beratPerDonat, value);
    }

    public decimal WastePersen
    {
        get => _wastePersen;
        set => this.RaiseAndSetIfChanged(ref _wastePersen, value);
    }

    public decimal TargetProfitPersen
    {
        get => _targetProfitPersen;
        set => this.RaiseAndSetIfChanged(ref _targetProfitPersen, value);
    }

    public int HariProduksiPerBulan
    {
        get => _hariProduksiPerBulan;
        set => this.RaiseAndSetIfChanged(ref _hariProduksiPerBulan, value);
    }
}

