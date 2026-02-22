using ReactiveUI;

namespace HPPDonat.Models;

public sealed class ResepItemModel : ReactiveObject
{
    private int _id;
    private int _bahanId;
    private string _namaBahan = string.Empty;
    private decimal _jumlahDipakai;
    private decimal _nettoPerPack = 1m;
    private decimal _hargaPerPack;
    private decimal _modalBahan;

    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public int BahanId
    {
        get => _bahanId;
        set => this.RaiseAndSetIfChanged(ref _bahanId, value);
    }

    public string NamaBahan
    {
        get => _namaBahan;
        set => this.RaiseAndSetIfChanged(ref _namaBahan, value);
    }

    public decimal JumlahDipakai
    {
        get => _jumlahDipakai;
        set => this.RaiseAndSetIfChanged(ref _jumlahDipakai, value);
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

    public decimal ModalBahan
    {
        get => _modalBahan;
        set => this.RaiseAndSetIfChanged(ref _modalBahan, value);
    }
}

