using ReactiveUI;

namespace HPPDonat.Models;

public sealed class BahanModel : ReactiveObject
{
    private int _id;
    private string _namaBahan = string.Empty;
    private decimal _nettoPerPack = 1m;
    private decimal _hargaPerPack;

    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public string NamaBahan
    {
        get => _namaBahan;
        set => this.RaiseAndSetIfChanged(ref _namaBahan, value);
    }

    public decimal NettoPerPack
    {
        get => _nettoPerPack;
        set => this.RaiseAndSetIfChanged(ref _nettoPerPack, value);
    }

    public decimal HargaPerPack
    {
        get => _hargaPerPack;
        set => this.RaiseAndSetIfChanged(ref _hargaPerPack, value);
    }
}

