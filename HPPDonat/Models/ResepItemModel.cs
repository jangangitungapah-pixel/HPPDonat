using ReactiveUI;

namespace HPPDonat.Models;

public sealed class ResepItemModel : ReactiveObject
{
    private int _id;
    private int _bahanId;
    private string _namaBahan = string.Empty;
    private string _satuan = "gram";
    private decimal _jumlahDipakai;
    private decimal _nettoPerPack = 1m;
    private decimal _hargaPerPack;
    private decimal _modalBahan;
    private decimal _kontribusiPersen;
    private string _validationMessage = "OK";

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

    public string Satuan
    {
        get => _satuan;
        set => this.RaiseAndSetIfChanged(ref _satuan, value);
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

    public decimal KontribusiPersen
    {
        get => _kontribusiPersen;
        set => this.RaiseAndSetIfChanged(ref _kontribusiPersen, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set => this.RaiseAndSetIfChanged(ref _validationMessage, value);
    }
}
