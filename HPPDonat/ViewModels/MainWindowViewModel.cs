using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using FluentAvalonia.UI.Controls;
using FiSymbol = FluentIcons.Common.Symbol;
using HPPDonat.Models;
using HPPDonat.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using SkiaSharp;

namespace HPPDonat.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IAppStateService _appStateService;
    private readonly ObservableCollection<double> _trendValues = [];

    private readonly ObservableAsPropertyHelper<ViewModelBase> _currentPage;
    private readonly ObservableAsPropertyHelper<string> _judulHalaman;
    private bool _isDarkMode;
    private NavigationItem? _selectedNavigationItem;

    public MainWindowViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        IAppStateService appStateService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _appStateService = appStateService;

        TrendSeries =
        [
            new LineSeries<double>
            {
                Values = _trendValues,
                GeometrySize = 0,
                LineSmoothness = 0.8,
                Stroke = new SolidColorPaint(new SKColor(13, 138, 208), 3),
                Fill = new SolidColorPaint(new SKColor(13, 138, 208, 50))
            }
        ];

        TrendXAxes =
        [
            new Axis
            {
                IsVisible = false,
                MinStep = 1
            }
        ];

        TrendYAxes =
        [
            new Axis
            {
                IsVisible = false
            }
        ];

        // Inisialisasi Menu Navigasi
        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new NavigationItem { Label = "Dashboard", Icon = FiSymbol.Home, ViewModelType = typeof(DashboardViewModel), ToolTip = "Ringkasan total modal, HPP final, harga jual, dan profit." },
            new NavigationItem { Label = "Master Bahan", Icon = FiSymbol.BoxMultiple, ViewModelType = typeof(MasterBahanViewModel), ToolTip = "Kelola data master bahan baku donat." },
            new NavigationItem { Label = "Resep Produksi", Icon = FiSymbol.ClipboardText, ViewModelType = typeof(ResepProduksiViewModel), ToolTip = "Atur jumlah bahan yang dipakai per batch produksi." },
            new NavigationItem { Label = "Topping", Icon = FiSymbol.Star, ViewModelType = typeof(ToppingViewModel), ToolTip = "Atur topping aktif dan biaya topping per donat." },
            new NavigationItem { Label = "Kalkulator", Icon = FiSymbol.Calculator, ViewModelType = typeof(KalkulatorProduksiViewModel), ToolTip = "Input jumlah donat, waste, dan target profit untuk simulasi harga jual." },
            new NavigationItem { Label = "Analisa Profit", Icon = FiSymbol.ChartMultiple, ViewModelType = typeof(AnalisaProfitViewModel), ToolTip = "Simulasi skenario profit untuk evaluasi bisnis." }
        };

        // Bind CurrentPage to NavigationService.CurrentViewModel
        _currentPage = _navigationService.CurrentViewModelChanged
            .ToProperty(this, x => x.CurrentPage);

        // Bind JudulHalaman to CurrentPage.Title
        _judulHalaman = _navigationService.CurrentViewModelChanged
            .Select(vm => vm?.Title ?? "HPP Donat Calculator")
            .ToProperty(this, x => x.JudulHalaman);

        DismissErrorCommand = ReactiveCommand.Create(() => _appStateService.ClearErrorStatus());

        _appStateService.Calculation.PropertyChanged += OnCalculationChanged;
        _appStateService.Status.PropertyChanged += OnStatusChanged;

        RefreshTrend();

        // Default navigation
        SelectedNavigationItem = NavigationItems[0];
    }

    public string NamaAplikasi => "HPP Donat Calculator";

    public ViewModelBase CurrentPage => _currentPage.Value;

    public string JudulHalaman => _judulHalaman.Value;

    public ObservableCollection<NavigationItem> NavigationItems { get; }
    public CalculationOutputModel Ringkasan => _appStateService.Calculation;
    public ProduksiSettingModel ProduksiSetting => _appStateService.ProduksiSetting;
    public IEnumerable<ISeries> TrendSeries { get; }
    public IEnumerable<Axis> TrendXAxes { get; }
    public IEnumerable<Axis> TrendYAxes { get; }
    public InfoBarSeverity StatusSeverity => Status.IsError ? InfoBarSeverity.Error : Status.IsBusy ? InfoBarSeverity.Informational : InfoBarSeverity.Success;
    public bool IsStatusOpen => Status.IsBusy || Status.IsError;
    public string StatusTitle => Status.IsError ? "Terjadi Masalah" : Status.IsBusy ? "Sedang Memproses" : "Status Sistem";

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedNavigationItem, value);
            if (value is not null)
            {
                _navigationService.NavigateTo(value.ViewModelType);
            }
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isDarkMode, value);
            _themeService.ApplyTheme(value);
        }
    }

    public AppStatusModel Status => _appStateService.Status;

    public ReactiveCommand<Unit, Unit> DismissErrorCommand { get; }

    private void OnCalculationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CalculationOutputModel.HppFinal)
            or nameof(CalculationOutputModel.HargaJual)
            or nameof(CalculationOutputModel.ProfitPerDonat)
            or nameof(CalculationOutputModel.EstimasiHarian)
            or nameof(CalculationOutputModel.EstimasiBulanan)
            or nameof(CalculationOutputModel.ProduksiEfektif))
        {
            RefreshTrend();
        }
    }

    private void OnStatusChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppStatusModel.IsBusy)
            or nameof(AppStatusModel.IsError))
        {
            this.RaisePropertyChanged(nameof(StatusSeverity));
            this.RaisePropertyChanged(nameof(IsStatusOpen));
            this.RaisePropertyChanged(nameof(StatusTitle));
        }
    }

    private void RefreshTrend()
    {
        var modal = (double)Ringkasan.HppFinal;
        var harga = (double)Ringkasan.HargaJual;
        var profit = (double)Ringkasan.ProfitPerDonat;
        var harian = (double)Ringkasan.EstimasiHarian;
        var bulanan = (double)Ringkasan.EstimasiBulanan;
        var output = (double)Ringkasan.ProduksiEfektif;

        _trendValues.Clear();
        _trendValues.Add(Math.Max(0, modal));
        _trendValues.Add(Math.Max(0, harga));
        _trendValues.Add(Math.Max(0, profit));
        _trendValues.Add(Math.Max(0, output));
        _trendValues.Add(Math.Max(0, harian));
        _trendValues.Add(Math.Max(0, bulanan));
    }
}
