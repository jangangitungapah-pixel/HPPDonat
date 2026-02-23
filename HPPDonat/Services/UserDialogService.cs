using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace HPPDonat.Services;

public sealed class UserDialogService : IUserDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
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

        await dialog.ShowDialog(desktop.MainWindow);
        return result;
    }

    private static Control BuildContent(string message, Action onConfirm, Action onCancel)
    {
        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };

        var yesButton = new Button
        {
            Content = "Ya, Hapus",
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 8, 0)
        };
        yesButton.Click += (_, _) => onConfirm();

        var noButton = new Button
        {
            Content = "Batal",
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
}
