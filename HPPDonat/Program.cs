using System.Globalization;
using Avalonia;
using Avalonia.ReactiveUI;

namespace HPPDonat;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var culture = CultureInfo.GetCultureInfo("id-ID");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .WithInterFont()
            .LogToTrace();
}
