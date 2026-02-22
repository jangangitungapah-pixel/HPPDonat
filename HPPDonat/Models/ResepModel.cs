using ReactiveUI;

namespace HPPDonat.Models;

public sealed class ResepModel : ReactiveObject
{
    private int _id;
    private int _bahanId;
    private decimal _jumlahDipakai;

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

    public decimal JumlahDipakai
    {
        get => _jumlahDipakai;
        set => this.RaiseAndSetIfChanged(ref _jumlahDipakai, value);
    }
}

