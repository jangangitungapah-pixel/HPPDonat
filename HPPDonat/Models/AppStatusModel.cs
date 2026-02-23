using ReactiveUI;

namespace HPPDonat.Models;

public sealed class AppStatusModel : ReactiveObject
{
    private string _message = "Siap digunakan.";
    private bool _isBusy;
    private bool _isError;
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public bool IsError
    {
        get => _isError;
        set => this.RaiseAndSetIfChanged(ref _isError, value);
    }

    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }
}
