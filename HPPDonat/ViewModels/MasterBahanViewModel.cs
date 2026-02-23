using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using HPPDonat.Helpers;
using HPPDonat.Models;
using HPPDonat.Services;
using ReactiveUI;

namespace HPPDonat.ViewModels;

public sealed class MasterBahanViewModel : ViewModelBase, IDisposable
{
    public override string Title => "Master Bahan";

    private const string SemuaSatuan = "Semua";

    private readonly IAppStateService _appStateService;
    private readonly IUserDialogService _dialogService;

    private BahanModel? _selectedBahan;
    private string _kataKunciFilter = string.Empty;
    private string _filterSatuan = SemuaSatuan;

    private string _namaBahanBaru = string.Empty;
    private string _satuanBahanBaru = "gram";
    private decimal _nettoPerPackBaru = 1000m;
    private decimal _hargaPerPackBaru;

    public MasterBahanViewModel(IAppStateService appStateService, IUserDialogService dialogService)
    {
        _appStateService = appStateService;
        _dialogService = dialogService;

        SatuanOptions = new ReadOnlyCollection<string>(new[]
        {
            "gram",
            "kg",
            "ml",
            "liter",
            "butir",
            "pcs",
            "sendok"
        });

        FilterSatuanOptions = new ReadOnlyCollection<string>(new[]
        {
            SemuaSatuan,
            "gram",
            "kg",
            "ml",
            "liter",
            "butir",
            "pcs",
            "sendok"
        });

        FilteredBahanItems = new ObservableCollection<BahanModel>();

        _appStateService.BahanItems.CollectionChanged += OnBahanCollectionChanged;
        foreach (var bahan in BahanItems)
        {
            bahan.PropertyChanged += OnBahanItemPropertyChanged;
        }

        TambahBahanCommand = ReactiveCommand.CreateFromTask(TambahBahanDariFormAsync);

        var canDeleteSelected = this
            .WhenAnyValue(viewModel => viewModel.SelectedBahan)
            .Select(bahan => bahan is not null);

        HapusBahanCommand = ReactiveCommand.CreateFromTask(HapusBahanTerpilihAsync, canDeleteSelected);

        HapusBahanItemCommand = ReactiveCommand.CreateFromTask<BahanModel?>(HapusBahanItemAsync);

        IsiFormDariBahanCommand = ReactiveCommand.Create<BahanModel?>(IsiFormDariBahan);

        ResetFormCommand = ReactiveCommand.Create(ResetForm);

        ClearFilterCommand = ReactiveCommand.Create(() =>
        {
            KataKunciFilter = string.Empty;
            FilterSatuan = SemuaSatuan;
        });

        RefreshFilteredItems();
    }

    public ObservableCollection<BahanModel> BahanItems => _appStateService.BahanItems;

    public ObservableCollection<BahanModel> FilteredBahanItems { get; }

    public ReadOnlyCollection<string> SatuanOptions { get; }

    public ReadOnlyCollection<string> FilterSatuanOptions { get; }

    public string KataKunciFilter
    {
        get => _kataKunciFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _kataKunciFilter, value);
            RefreshFilteredItems();
        }
    }

    public string FilterSatuan
    {
        get => _filterSatuan;
        set
        {
            this.RaiseAndSetIfChanged(ref _filterSatuan, value);
            RefreshFilteredItems();
        }
    }

    public string NamaBahanBaru
    {
        get => _namaBahanBaru;
        set => this.RaiseAndSetIfChanged(ref _namaBahanBaru, value);
    }

    public string SatuanBahanBaru
    {
        get => _satuanBahanBaru;
        set => this.RaiseAndSetIfChanged(ref _satuanBahanBaru, value);
    }

    public decimal NettoPerPackBaru
    {
        get => _nettoPerPackBaru;
        set => this.RaiseAndSetIfChanged(ref _nettoPerPackBaru, value);
    }

    public decimal HargaPerPackBaru
    {
        get => _hargaPerPackBaru;
        set => this.RaiseAndSetIfChanged(ref _hargaPerPackBaru, value);
    }

    public int JumlahBahanMaster => BahanItems.Count;

    public int JumlahBahanDitampilkan => FilteredBahanItems.Count;

    public bool HasBahanMaster => JumlahBahanMaster > 0;

    public bool HasFilteredItems => JumlahBahanDitampilkan > 0;

    public decimal RataHargaPerPack => BahanItems.Count == 0 ? 0m : BahanItems.Average(item => item.HargaPerPack);

    public decimal RataNettoPerPack => BahanItems.Count == 0 ? 0m : BahanItems.Average(item => item.NettoPerPack);

    public bool HasFilterAktif =>
        !string.IsNullOrWhiteSpace(KataKunciFilter) ||
        !string.Equals(FilterSatuan, SemuaSatuan, StringComparison.OrdinalIgnoreCase);

    public BahanModel? SelectedBahan
    {
        get => _selectedBahan;
        set => this.RaiseAndSetIfChanged(ref _selectedBahan, value);
    }

    public ReactiveCommand<Unit, Unit> TambahBahanCommand { get; }

    public ReactiveCommand<Unit, Unit> HapusBahanCommand { get; }

    public ReactiveCommand<BahanModel?, Unit> HapusBahanItemCommand { get; }

    public ReactiveCommand<BahanModel?, Unit> IsiFormDariBahanCommand { get; }

    public ReactiveCommand<Unit, Unit> ResetFormCommand { get; }

    public ReactiveCommand<Unit, Unit> ClearFilterCommand { get; }

    private async Task TambahBahanDariFormAsync()
    {
        var nama = InputValidator.NormalizeName(NamaBahanBaru, "Bahan Baru");
        var satuan = InputValidator.NormalizeSatuan(SatuanBahanBaru);
        var netto = InputValidator.ClampPositive(NettoPerPackBaru, 1m);
        var harga = InputValidator.ClampNonNegative(HargaPerPackBaru);

        var idSebelum = BahanItems.Select(item => item.Id).ToHashSet();

        await _appStateService.TambahBahanAsync();

        var bahanBaru = BahanItems.FirstOrDefault(item => !idSebelum.Contains(item.Id)) ?? BahanItems.LastOrDefault();
        if (bahanBaru is null)
        {
            return;
        }

        bahanBaru.NamaBahan = nama;
        bahanBaru.Satuan = satuan;
        bahanBaru.NettoPerPack = netto;
        bahanBaru.HargaPerPack = harga;

        SelectedBahan = bahanBaru;

        if (!MatchesFilter(bahanBaru))
        {
            KataKunciFilter = string.Empty;
            FilterSatuan = SemuaSatuan;
        }

        NamaBahanBaru = string.Empty;
        SatuanBahanBaru = "gram";
        NettoPerPackBaru = 1000m;
        HargaPerPackBaru = 0m;
    }

    private async Task HapusBahanTerpilihAsync()
    {
        await HapusBahanItemAsync(SelectedBahan);
    }

    private async Task HapusBahanItemAsync(BahanModel? bahan)
    {
        if (bahan is null)
        {
            return;
        }

        var disetujui = await _dialogService.ConfirmAsync(
            "Konfirmasi Hapus Bahan",
            $"Yakin ingin menghapus bahan '{bahan.NamaBahan}'? Data resep terkait juga akan terhapus.");

        if (!disetujui)
        {
            return;
        }

        await _appStateService.HapusBahanAsync(bahan);
    }

    private void IsiFormDariBahan(BahanModel? bahan)
    {
        if (bahan is null)
        {
            return;
        }

        NamaBahanBaru = bahan.NamaBahan;
        SatuanBahanBaru = bahan.Satuan;
        NettoPerPackBaru = bahan.NettoPerPack;
        HargaPerPackBaru = bahan.HargaPerPack;
    }

    private void ResetForm()
    {
        NamaBahanBaru = string.Empty;
        SatuanBahanBaru = "gram";
        NettoPerPackBaru = 1000m;
        HargaPerPackBaru = 0m;
    }

    private void OnBahanCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<BahanModel>())
            {
                oldItem.PropertyChanged -= OnBahanItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<BahanModel>())
            {
                newItem.PropertyChanged += OnBahanItemPropertyChanged;
            }
        }

        if (SelectedBahan is not null && !BahanItems.Contains(SelectedBahan))
        {
            SelectedBahan = null;
        }

        RefreshFilteredItems();
    }

    private void OnBahanItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BahanModel.NamaBahan)
            or nameof(BahanModel.Satuan)
            or nameof(BahanModel.NettoPerPack)
            or nameof(BahanModel.HargaPerPack))
        {
            RefreshFilteredItems();
        }
    }

    private void RefreshFilteredItems()
    {
        var filtered = BahanItems
            .Where(MatchesFilter)
            .OrderBy(item => item.NamaBahan, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        FilteredBahanItems.Clear();
        foreach (var item in filtered)
        {
            FilteredBahanItems.Add(item);
        }

        if (SelectedBahan is not null && !FilteredBahanItems.Contains(SelectedBahan))
        {
            SelectedBahan = null;
        }

        this.RaisePropertyChanged(nameof(JumlahBahanMaster));
        this.RaisePropertyChanged(nameof(JumlahBahanDitampilkan));
        this.RaisePropertyChanged(nameof(HasBahanMaster));
        this.RaisePropertyChanged(nameof(HasFilteredItems));
        this.RaisePropertyChanged(nameof(RataHargaPerPack));
        this.RaisePropertyChanged(nameof(RataNettoPerPack));
        this.RaisePropertyChanged(nameof(HasFilterAktif));
    }

    private bool MatchesFilter(BahanModel item)
    {
        var kataKunci = KataKunciFilter.Trim();

        var matchKataKunci = string.IsNullOrWhiteSpace(kataKunci)
            || item.NamaBahan.Contains(kataKunci, StringComparison.CurrentCultureIgnoreCase)
            || item.Satuan.Contains(kataKunci, StringComparison.CurrentCultureIgnoreCase);

        var matchSatuan = string.Equals(FilterSatuan, SemuaSatuan, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Satuan, FilterSatuan, StringComparison.OrdinalIgnoreCase);

        return matchKataKunci && matchSatuan;
    }

    public void Dispose()
    {
        _appStateService.BahanItems.CollectionChanged -= OnBahanCollectionChanged;

        foreach (var bahan in BahanItems)
        {
            bahan.PropertyChanged -= OnBahanItemPropertyChanged;
        }
    }
}
