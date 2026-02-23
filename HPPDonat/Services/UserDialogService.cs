using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace HPPDonat.Services;

public sealed class UserDialogService : IUserDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Ya", string cancelText = "Batal")
    {
        if (!TryGetMainWindow(out var mainWindow))
        {
            return false;
        }

        var result = false;

        var dialog = new Window
        {
            Title = title,
            Width = 430,
            Height = 210,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        dialog.Content = BuildContent(
            message,
            confirmText,
            cancelText,
            onConfirm: () =>
            {
                result = true;
                dialog.Close();
            },
            onCancel: () =>
            {
                result = false;
                dialog.Close();
            });

        await dialog.ShowDialog(mainWindow);
        return result;
    }

    public async Task<string?> PromptTextAsync(string title, string message, string initialValue = "", string confirmText = "Simpan", string cancelText = "Batal")
    {
        if (!TryGetMainWindow(out var mainWindow))
        {
            return null;
        }

        string? result = null;

        var input = new TextBox
        {
            Text = initialValue,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var dialog = new Window
        {
            Title = title,
            Width = 480,
            Height = 240,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        dialog.Content = BuildPromptContent(
            message,
            input,
            confirmText,
            cancelText,
            onConfirm: () =>
            {
                result = input.Text;
                dialog.Close();
            },
            onCancel: () =>
            {
                result = null;
                dialog.Close();
            });

        dialog.Opened += (_, _) =>
        {
            input.Focus();
            input.SelectAll();
        };

        await dialog.ShowDialog(mainWindow);
        return result;
    }

    private static bool TryGetMainWindow(out Window mainWindow)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            mainWindow = desktop.MainWindow;
            return true;
        }

        mainWindow = null!;
        return false;
    }

    private static Control BuildContent(string message, string confirmText, string cancelText, Action onConfirm, Action onCancel)
    {
        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };

        var yesButton = new Button
        {
            Content = confirmText,
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 8, 0)
        };
        yesButton.Click += (_, _) => onConfirm();

        var noButton = new Button
        {
            Content = cancelText,
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        noButton.Click += (_, _) => onCancel();

        return new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 8,
            Children =
            {
                textBlock,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children =
                    {
                        yesButton,
                        noButton
                    }
                }
            }
        };
    }

    private static Control BuildPromptContent(
        string message,
        TextBox input,
        string confirmText,
        string cancelText,
        Action onConfirm,
        Action onCancel)
    {
        var messageBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        };

        var confirmButton = new Button
        {
            Content = confirmText,
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 8, 0)
        };
        confirmButton.Click += (_, _) => onConfirm();

        var cancelButton = new Button
        {
            Content = cancelText,
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        cancelButton.Click += (_, _) => onCancel();

        return new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                messageBlock,
                input,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 8, 0, 0),
                    Children =
                    {
                        confirmButton,
                        cancelButton
                    }
                }
            }
        };
    }
}
