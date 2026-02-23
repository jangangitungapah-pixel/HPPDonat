namespace HPPDonat.Services;

public interface IUserDialogService
{
    Task<bool> ConfirmAsync(string title, string message);
}
