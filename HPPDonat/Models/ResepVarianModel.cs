using ReactiveUI;

namespace HPPDonat.Models;

public sealed class ResepVarianModel : ReactiveObject
{
    private int _id;
    private string _namaVarian = "Varian Baru";
    private bool _isActive;

    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public string NamaVarian
    {
        get => _namaVarian;
        set => this.RaiseAndSetIfChanged(ref _namaVarian, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }
}
