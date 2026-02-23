using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using HPPDonat.Helpers;
using HPPDonat.Models;
using HPPDonat.Services;
using ReactiveUI;

namespace HPPDonat.ViewModels;

public sealed class ResepProduksiViewModel : ViewModelBase, IDisposable
{
    private const string UrutNamaAsc = "Nama Bahan (A-Z)";
    private const string UrutJumlahDesc = "Jumlah Dipakai Tertinggi";
    private const string UrutKontribusiDesc = "Kontribusi Tertinggi";
    private const string UrutModalDesc = "Modal Bahan Tertinggi";
    private const string UrutMasalahDulu = "Masalah Dulu";

    public override string Title => "Resep Produksi";

    private readonly IAppStateService _appStateService;
    private readonly IUserDialogService _dialogService;
    private ResepVarianModel? _selectedVarian;
    private ResepItemModel? _selectedResepItem;
    private bool _isSyncingSelectedVarian;
    private bool _isBulkUpdatingResep;
    private string _kataKunciResep = string.Empty;
    private string _urutanResep = UrutNamaAsc;
    private bool _filterHanyaBermasalah;

    public ResepProduksiViewModel(IAppStateService appStateService, IUserDialogService dialogService)
    {
        _appStateService = appStateService;
        _dialogService = dialogService;

        _selectedVarian = _appStateService.VarianAktif;

        UrutanResepOptions = new ReadOnlyCollection<string>(new[]
        {
            UrutNamaAsc,
            UrutJumlahDesc,
            UrutKontribusiDesc,
            UrutModalDesc,
            UrutMasalahDulu
        });

        FilteredResepItems = new ObservableCollection<ResepItemModel>();

        _appStateService.ResepItems.CollectionChanged += OnResepCollectionChanged;
        _appStateService.ResepVarianItems.CollectionChanged += OnVarianCollectionChanged;
        _appStateService.PropertyChanged += OnAppStatePropertyChanged;

        foreach (var item in ResepItems)
        {
            item.PropertyChanged += OnResepItemPropertyChanged;
        }

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

        UbahNamaVarianCommand = ReactiveCommand.CreateFromTask(UbahNamaVarianAsync);

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
                $"Yakin ingin menghapus varian resep '{SelectedVarian.NamaVarian}'?",
                "Ya, Hapus",
                "Batal");

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
                "Yakin ingin reset seluruh JumlahDipakai pada varian aktif ke 0?",
                "Ya, Reset",
                "Batal");

            if (!disetujui)
            {
                return;
            }

            await _appStateService.ResetResepAktifAsync();
        });

        ClearFilterResepCommand = ReactiveCommand.Create(() =>
        {
            KataKunciResep = string.Empty;
            FilterHanyaBermasalah = false;
            UrutanResep = UrutNamaAsc;
        });

        TerapkanSkalaCepatCommand = ReactiveCommand.Create<string?>(TerapkanSkalaCepat);

        RefreshFilteredResepItems();
    }

    public ObservableCollection<ResepItemModel> ResepItems => _appStateService.ResepItems;

    public ObservableCollection<ResepItemModel> FilteredResepItems { get; }

    public ObservableCollection<ResepVarianModel> ResepVarianItems => _appStateService.ResepVarianItems;

    public ReadOnlyCollection<string> UrutanResepOptions { get; }

    public ProduksiSettingModel ProduksiSetting => _appStateService.ProduksiSetting;

    public CalculationOutputModel Ringkasan => _appStateService.Calculation;

    public bool HasResepItems => ResepItems.Count > 0;

    public int JumlahVarianResep => ResepVarianItems.Count;

    public string KataKunciResep
    {
        get => _kataKunciResep;
        set
        {
            this.RaiseAndSetIfChanged(ref _kataKunciResep, value);
            RefreshFilteredResepItems();
        }
    }

    public string UrutanResep
    {
        get => _urutanResep;
        set
        {
            this.RaiseAndSetIfChanged(ref _urutanResep, value);
            RefreshFilteredResepItems();
        }
    }

    public bool FilterHanyaBermasalah
    {
        get => _filterHanyaBermasalah;
        set
        {
            this.RaiseAndSetIfChanged(ref _filterHanyaBermasalah, value);
            RefreshFilteredResepItems();
        }
    }

    public bool HasFilterAktif =>
        !string.IsNullOrWhiteSpace(KataKunciResep)
        || FilterHanyaBermasalah
        || !string.Equals(UrutanResep, UrutNamaAsc, StringComparison.Ordinal);

    public int JumlahResepDitampilkan => FilteredResepItems.Count;

    public int JumlahBarisResep => ResepItems.Count;

    public int JumlahBarisValid => ResepItems.Count(IsResepItemValid);

    public int JumlahBelumDipakai => ResepItems.Count(item => item.JumlahDipakai <= 0m);

    public int JumlahMasterTidakValid => ResepItems.Count(item => item.NettoPerPack <= 0m || item.HargaPerPack < 0m);

    public int JumlahBarisBermasalah => Math.Max(0, JumlahBarisResep - JumlahBarisValid);

    public bool AdaMasalahResep => JumlahBarisBermasalah > 0;

    public string RingkasanFilterResep =>
        $"{JumlahResepDitampilkan:N0} dari {JumlahBarisResep:N0} bahan ditampilkan";

    public string RingkasanKesehatan =>
        AdaMasalahResep
            ? $"{JumlahBarisBermasalah:N0} baris perlu dicek sebelum finalisasi HPP."
            : "Semua baris resep valid dan siap dipakai.";

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

    public ResepItemModel? SelectedResepItem
    {
        get => _selectedResepItem;
        set => this.RaiseAndSetIfChanged(ref _selectedResepItem, value);
    }

    public ReactiveCommand<Unit, Unit> TambahBahanCepatCommand { get; }

    public ReactiveCommand<Unit, Unit> TambahVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> DuplikasiVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> UbahNamaVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> HapusVarianCommand { get; }

    public ReactiveCommand<Unit, Unit> ResetResepCommand { get; }

    public ReactiveCommand<Unit, Unit> ClearFilterResepCommand { get; }

    public ReactiveCommand<string?, Unit> TerapkanSkalaCepatCommand { get; }

    private async Task UbahVarianAsync(ResepVarianModel varian)
    {
        await _appStateService.PilihVarianResepAsync(varian);
        SyncSelectedVarianWithAppState();
    }

    private async Task UbahNamaVarianAsync()
    {
        if (SelectedVarian is null)
        {
            return;
        }

        var namaBaru = await _dialogService.PromptTextAsync(
            "Ubah Nama Varian",
            $"Masukkan nama baru untuk varian '{SelectedVarian.NamaVarian}'.",
            SelectedVarian.NamaVarian,
            "Simpan",
            "Batal");

        if (namaBaru is null)
        {
            return;
        }

        await _appStateService.UbahNamaVarianResepAsync(SelectedVarian, namaBaru);
        SyncSelectedVarianWithAppState();
    }

    private void TerapkanSkalaCepat(string? faktorText)
    {
        if (string.IsNullOrWhiteSpace(faktorText) || ResepItems.Count == 0)
        {
            return;
        }

        if (!decimal.TryParse(faktorText, NumberStyles.Number, CultureInfo.InvariantCulture, out var faktor)
            && !decimal.TryParse(faktorText, NumberStyles.Number, CultureInfo.CurrentCulture, out faktor))
        {
            return;
        }

        faktor = InputValidator.Clamp(faktor, 0.01m, 100m);

        _isBulkUpdatingResep = true;
        try
        {
            foreach (var item in ResepItems)
            {
                var scaled = Math.Round(item.JumlahDipakai * faktor, 2, MidpointRounding.AwayFromZero);
                item.JumlahDipakai = InputValidator.ClampNonNegative(scaled);
            }
        }
        finally
        {
            _isBulkUpdatingResep = false;
        }

        RefreshFilteredResepItems();
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
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<ResepItemModel>())
            {
                oldItem.PropertyChanged -= OnResepItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<ResepItemModel>())
            {
                newItem.PropertyChanged += OnResepItemPropertyChanged;
            }
        }

        if (SelectedResepItem is not null && !ResepItems.Contains(SelectedResepItem))
        {
            SelectedResepItem = null;
        }

        this.RaisePropertyChanged(nameof(HasResepItems));
        RefreshFilteredResepItems();
    }

    private void OnVarianCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RaisePropertyChanged(nameof(JumlahVarianResep));
    }

    private void OnResepItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isBulkUpdatingResep)
        {
            return;
        }

        if (e.PropertyName is nameof(ResepItemModel.NamaBahan)
            or nameof(ResepItemModel.Satuan)
            or nameof(ResepItemModel.JumlahDipakai)
            or nameof(ResepItemModel.ModalBahan)
            or nameof(ResepItemModel.KontribusiPersen)
            or nameof(ResepItemModel.ValidationMessage)
            or nameof(ResepItemModel.NettoPerPack)
            or nameof(ResepItemModel.HargaPerPack))
        {
            RefreshFilteredResepItems();
        }
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppStateService.VarianAktif))
        {
            SyncSelectedVarianWithAppState();
        }
    }

    private void RefreshFilteredResepItems()
    {
        var filtered = ResepItems
            .Where(MatchesFilter)
            .ToList();

        filtered = UrutanResep switch
        {
            UrutJumlahDesc => filtered
                .OrderByDescending(item => item.JumlahDipakai)
                .ThenBy(item => item.NamaBahan, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            UrutKontribusiDesc => filtered
                .OrderByDescending(item => item.KontribusiPersen)
                .ThenBy(item => item.NamaBahan, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            UrutModalDesc => filtered
                .OrderByDescending(item => item.ModalBahan)
                .ThenBy(item => item.NamaBahan, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            UrutMasalahDulu => filtered
                .OrderBy(item => IsResepItemValid(item) ? 1 : 0)
                .ThenByDescending(item => item.KontribusiPersen)
                .ThenBy(item => item.NamaBahan, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            _ => filtered
                .OrderBy(item => item.NamaBahan, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
        };

        FilteredResepItems.Clear();
        foreach (var item in filtered)
        {
            FilteredResepItems.Add(item);
        }

        if (SelectedResepItem is not null && !FilteredResepItems.Contains(SelectedResepItem))
        {
            SelectedResepItem = null;
        }

        this.RaisePropertyChanged(nameof(JumlahResepDitampilkan));
        this.RaisePropertyChanged(nameof(HasFilterAktif));
        this.RaisePropertyChanged(nameof(RingkasanFilterResep));
        this.RaisePropertyChanged(nameof(JumlahBarisResep));
        this.RaisePropertyChanged(nameof(JumlahBarisValid));
        this.RaisePropertyChanged(nameof(JumlahBelumDipakai));
        this.RaisePropertyChanged(nameof(JumlahMasterTidakValid));
        this.RaisePropertyChanged(nameof(JumlahBarisBermasalah));
        this.RaisePropertyChanged(nameof(AdaMasalahResep));
        this.RaisePropertyChanged(nameof(RingkasanKesehatan));
    }

    private bool MatchesFilter(ResepItemModel item)
    {
        var kataKunci = KataKunciResep.Trim();

        var matchKataKunci = string.IsNullOrWhiteSpace(kataKunci)
            || item.NamaBahan.Contains(kataKunci, StringComparison.CurrentCultureIgnoreCase)
            || item.Satuan.Contains(kataKunci, StringComparison.CurrentCultureIgnoreCase);

        var matchStatus = !FilterHanyaBermasalah || !IsResepItemValid(item);
        return matchKataKunci && matchStatus;
    }

    private static bool IsResepItemValid(ResepItemModel item)
    {
        return string.Equals(item.ValidationMessage, "OK", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _appStateService.ResepItems.CollectionChanged -= OnResepCollectionChanged;
        _appStateService.ResepVarianItems.CollectionChanged -= OnVarianCollectionChanged;
        _appStateService.PropertyChanged -= OnAppStatePropertyChanged;

        foreach (var item in ResepItems)
        {
            item.PropertyChanged -= OnResepItemPropertyChanged;
        }
    }
}
