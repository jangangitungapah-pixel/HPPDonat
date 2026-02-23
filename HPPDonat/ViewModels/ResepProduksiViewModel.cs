using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using HPPDonat.Models;
using HPPDonat.Services;
using ReactiveUI;

namespace HPPDonat.ViewModels;

public sealed class ResepProduksiViewModel : ViewModelBase, IDisposable
{
    public override string Title => "Resep Produksi";

    private readonly IAppStateService _appStateService;
    private readonly IUserDialogService _dialogService;
    private ResepVarianModel? _selectedVarian;
    private bool _isSyncingSelectedVarian;

    public ResepProduksiViewModel(IAppStateService appStateService, IUserDialogService dialogService)
    {
        _appStateService = appStateService;
        _dialogService = dialogService;

        _selectedVarian = _appStateService.VarianAktif;

        _appStateService.ResepItems.CollectionChanged += OnResepCollectionChanged;
        _appStateService.PropertyChanged += OnAppStatePropertyChanged;

        TambahBahanCepatCommand = ReactiveCommand.CreateFromTask(() => _appStateService.TambahBahanAsync());

        TambahVarianCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await _appStateService.TambahVarianResepAsync();
            SyncSelectedVarianWithAppState();
        });

        DuplikasiVarianCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await _appStateService.DuplikasiVarianAktifAsync();
            SyncSelectedVarianWithAppState();
        });

        var varianCountObservable = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => ResepVarianItems.CollectionChanged += handler,
                handler => ResepVarianItems.CollectionChanged -= handler)
            .Select(_ => ResepVarianItems.Count)
            .StartWith(ResepVarianItems.Count);

        var canDeleteVarian = this
            .WhenAnyValue(viewModel => viewModel.SelectedVarian)
            .CombineLatest(varianCountObservable, (varian, count) => varian is not null && count > 1);

        HapusVarianCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedVarian is null)
            {
                return;
            }

            var disetujui = await _dialogService.ConfirmAsync(
                "Konfirmasi Hapus Varian",
                $"Yakin ingin menghapus varian resep '{SelectedVarian.NamaVarian}'?");

            if (!disetujui)
            {
                return;
            }

            await _appStateService.HapusVarianResepAsync(SelectedVarian);
            SyncSelectedVarianWithAppState();
        }, canDeleteVarian);

        ResetResepCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var disetujui = await _dialogService.ConfirmAsync(
                "Konfirmasi Reset Resep",
                "Yakin ingin reset seluruh JumlahDipakai pada varian aktif ke 0?");

            if (!disetujui)
            {
                return;
            }

            await _appStateService.ResetResepAktifAsync();
        });
    }

    public ObservableCollection<ResepItemModel> ResepItems => _appStateService.ResepItems;

    public ObservableCollection<ResepVarianModel> ResepVarianItems => _appStateService.ResepVarianItems;

    public ProduksiSettingModel ProduksiSetting => _appStateService.ProduksiSetting;

    public CalculationOutputModel Ringkasan => _appStateService.Calculation;

    public bool HasResepItems => ResepItems.Count > 0;

    public ResepVarianModel? SelectedVarian
    {
        get => _selectedVarian;
        set
        {
            var previousId = _selectedVarian?.Id;
            this.RaiseAndSetIfChanged(ref _selectedVarian, value);

            if (_isSyncingSelectedVarian)
            {
                return;
            }

            if (value is null || previousId == value.Id || _appStateService.VarianAktif?.Id == value.Id)
            {
                return;
            }

            _ = UbahVarianAsync(value);
        }
    }

    public ReactiveCommand<Unit, Unit> TambahBahanCepatCommand { get; }

    public ReactiveCommand<Unit, Unit> TambahVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> DuplikasiVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> HapusVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> ResetResepCommand { get; }

    private async Task UbahVarianAsync(ResepVarianModel varian)
    {
        await _appStateService.PilihVarianResepAsync(varian);
        SyncSelectedVarianWithAppState();
    }

    private void SyncSelectedVarianWithAppState()
    {
        var activeVarian = _appStateService.VarianAktif;
        if (activeVarian is null)
        {
            return;
        }

        var varianDiList = ResepVarianItems.FirstOrDefault(item => item.Id == activeVarian.Id) ?? activeVarian;
        if (_selectedVarian?.Id == varianDiList.Id)
        {
            return;
        }

        _isSyncingSelectedVarian = true;
        this.RaiseAndSetIfChanged(ref _selectedVarian, varianDiList, nameof(SelectedVarian));
        _isSyncingSelectedVarian = false;
    }

    private void OnResepCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RaisePropertyChanged(nameof(HasResepItems));
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppStateService.VarianAktif))
        {
            SyncSelectedVarianWithAppState();
        }
    }

    public void Dispose()
    {
        _appStateService.ResepItems.CollectionChanged -= OnResepCollectionChanged;
        _appStateService.PropertyChanged -= OnAppStatePropertyChanged;
    }
}