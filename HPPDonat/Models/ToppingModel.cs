using ReactiveUI;

namespace HPPDonat.Models;

public sealed class ToppingModel : ReactiveObject
{
    private int _id;
    private string _namaTopping = string.Empty;
    private decimal _biayaPerDonat;
    private bool _isActive = true;

    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public string NamaTopping
    {
        get => _namaTopping;
        set => this.RaiseAndSetIfChanged(ref _namaTopping, value);
    }

    public decimal BiayaPerDonat
    {
        get => _biayaPerDonat;
        set => this.RaiseAndSetIfChanged(ref _biayaPerDonat, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }
}

