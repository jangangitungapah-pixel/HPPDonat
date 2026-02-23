using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(HPPDonat.Tests.HeadlessTestAppBuilder))]

namespace HPPDonat.Tests;

public static class HeadlessTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<HPPDonat.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true
            })
            .WithInterFont();
    }
}
