using Avalonia;
using Avalonia.Styling;

namespace HPPDonat.Services;

public sealed class ThemeService : IThemeService
{
    public void ApplyTheme(bool useDarkMode)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = useDarkMode
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}

