using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HPPDonat.Data;
using HPPDonat.Services;
using HPPDonat.ViewModels;
using HPPDonat.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HPPDonat;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        var databaseInitializer = Services.GetRequiredService<DatabaseInitializer>();
        databaseInitializer.InitializeAsync().GetAwaiter().GetResult();

        var appStateService = Services.GetRequiredService<IAppStateService>();
        appStateService.InitializeAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.Exit += (_, _) =>
            {
                if (Services is IDisposable disposableServices)
                {
                    disposableServices.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DatabaseInitializer>();

        services.AddSingleton<IBahanRepository, BahanRepository>();
        services.AddSingleton<IResepRepository, ResepRepository>();
        services.AddSingleton<IResepVarianRepository, ResepVarianRepository>();
        services.AddSingleton<IToppingRepository, ToppingRepository>();
        services.AddSingleton<IProduksiSettingRepository, ProduksiSettingRepository>();
        services.AddSingleton<IProductionDataCoordinator, ProductionDataCoordinator>();

        services.AddSingleton<IRoundingService, RoundingService>();
        services.AddSingleton<ICostCalculationService, CostCalculationService>();
        services.AddSingleton<IProductionCalculationService, ProductionCalculationService>();
        services.AddSingleton<IProfitService, ProfitService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IUserDialogService, UserDialogService>();
        services.AddSingleton<IAppStateService, AppStateService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MasterBahanViewModel>();
        services.AddSingleton<ResepProduksiViewModel>();
        services.AddSingleton<ToppingViewModel>();
        services.AddSingleton<KalkulatorProduksiViewModel>();
        services.AddSingleton<AnalisaProfitViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }
}

