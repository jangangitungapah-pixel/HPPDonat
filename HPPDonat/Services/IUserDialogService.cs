namespace HPPDonat.Services;

public interface IUserDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Ya", string cancelText = "Batal");
    Task<string?> PromptTextAsync(string title, string message, string initialValue = "", string confirmText = "Simpan", string cancelText = "Batal");
}
