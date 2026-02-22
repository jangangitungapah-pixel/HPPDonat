using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using HPPDonat.Data;
using HPPDonat.Models;
using ReactiveUI;

namespace HPPDonat.Services;

public sealed class AppStateService : ReactiveObject, IAppStateService, IDisposable
{
    private readonly IBahanRepository _bahanRepository;
    private readonly IResepRepository _resepRepository;
    private readonly IToppingRepository _toppingRepository;
    private readonly IProduksiSettingRepository _produksiSettingRepository;
    private readonly ICostCalculationService _costCalculationService;
    private readonly IProductionCalculationService _productionCalculationService;
    private readonly IProfitService _profitService;

    private bool _isInitializing;
    private ProduksiSettingModel _produksiSetting = new();

    public AppStateService(
        IBahanRepository bahanRepository,
        IResepRepository resepRepository,
        IToppingRepository toppingRepository,
        IProduksiSettingRepository produksiSettingRepository,
        ICostCalculationService costCalculationService,
        IProductionCalculationService productionCalculationService,
        IProfitService profitService)
    {
        _bahanRepository = bahanRepository;
        _resepRepository = resepRepository;
        _toppingRepository = toppingRepository;
        _produksiSettingRepository = produksiSettingRepository;
        _costCalculationService = costCalculationService;
        _productionCalculationService = productionCalculationService;
        _profitService = profitService;

        BahanItems = new ObservableCollection<BahanModel>();
        ResepItems = new ObservableCollection<ResepItemModel>();
        ToppingItems = new ObservableCollection<ToppingModel>();
        Calculation = new CalculationOutputModel();
    }

    public ObservableCollection<BahanModel> BahanItems { get; }

    public ObservableCollection<ResepItemModel> ResepItems { get; }

    public ObservableCollection<ToppingModel> ToppingItems { get; }

    public ProduksiSettingModel ProduksiSetting
    {
        get => _produksiSetting;
        private set => this.RaiseAndSetIfChanged(ref _produksiSetting, value);
    }

    public CalculationOutputModel Calculation { get; }

    public async Task InitializeAsync()
    {
        _isInitializing = true;

        DetachAllHandlers();
        BahanItems.Clear();
        ResepItems.Clear();
        ToppingItems.Clear();

        var bahanList = await _bahanRepository.GetAllAsync();
        var resepList = await _resepRepository.GetAllAsync();
        var resepMap = resepList.ToDictionary(resep => resep.BahanId);

        foreach (var bahan in bahanList)
        {
            BahanItems.Add(bahan);
            AttachBahanHandler(bahan);

            if (!resepMap.TryGetValue(bahan.Id, out var resep))
            {
                resep = await _resepRepository.AddAsync(new ResepModel
                {
                    BahanId = bahan.Id,
                    JumlahDipakai = 0m
                });
            }

            var resepItem = new ResepItemModel
            {
                Id = resep.Id,
                BahanId = bahan.Id,
                NamaBahan = bahan.NamaBahan,
                NettoPerPack = bahan.NettoPerPack,
                HargaPerPack = bahan.HargaPerPack,
                JumlahDipakai = resep.JumlahDipakai
            };

            ResepItems.Add(resepItem);
            AttachResepHandler(resepItem);
        }

        var toppingList = await _toppingRepository.GetAllAsync();
        foreach (var topping in toppingList)
        {
            ToppingItems.Add(topping);
            AttachToppingHandler(topping);
        }

        ProduksiSetting = await _produksiSettingRepository.GetOrCreateAsync();
        AttachProduksiSettingHandler(ProduksiSetting);

        _isInitializing = false;
        Recalculate();
    }

    public async Task TambahBahanAsync()
    {
        var bahanBaru = await _bahanRepository.AddAsync(new BahanModel
        {
            NamaBahan = "Bahan Baru",
            NettoPerPack = 1000m,
            HargaPerPack = 0m
        });

        var resepBaru = await _resepRepository.AddAsync(new ResepModel
        {
            BahanId = bahanBaru.Id,
            JumlahDipakai = 0m
        });

        BahanItems.Add(bahanBaru);
        AttachBahanHandler(bahanBaru);

        var resepItem = new ResepItemModel
        {
            Id = resepBaru.Id,
            BahanId = bahanBaru.Id,
            NamaBahan = bahanBaru.NamaBahan,
            NettoPerPack = bahanBaru.NettoPerPack,
            HargaPerPack = bahanBaru.HargaPerPack,
            JumlahDipakai = 0m
        };

        ResepItems.Add(resepItem);
        AttachResepHandler(resepItem);

        Recalculate();
    }

    public async Task HapusBahanAsync(BahanModel? bahan)
    {
        if (bahan is null)
        {
            return;
        }

        await _resepRepository.DeleteByBahanIdAsync(bahan.Id);
        await _bahanRepository.DeleteAsync(bahan.Id);

        DetachBahanHandler(bahan);
        BahanItems.Remove(bahan);

        var resepItem = ResepItems.FirstOrDefault(item => item.BahanId == bahan.Id);
        if (resepItem is not null)
        {
            DetachResepHandler(resepItem);
            ResepItems.Remove(resepItem);
        }

        Recalculate();
    }

    public async Task TambahToppingAsync()
    {
        var toppingBaru = await _toppingRepository.AddAsync(new ToppingModel
        {
            NamaTopping = "Topping Baru",
            BiayaPerDonat = 0m,
            IsActive = true
        });

        ToppingItems.Add(toppingBaru);
        AttachToppingHandler(toppingBaru);
        Recalculate();
    }

    public async Task HapusToppingAsync(ToppingModel? topping)
    {
        if (topping is null)
        {
            return;
        }

        await _toppingRepository.DeleteAsync(topping.Id);
        DetachToppingHandler(topping);
        ToppingItems.Remove(topping);
        Recalculate();
    }

    public void Dispose()
    {
        DetachAllHandlers();
    }

    private void AttachBahanHandler(BahanModel bahan)
    {
        bahan.PropertyChanged += OnBahanPropertyChanged;
    }

    private void AttachResepHandler(ResepItemModel resepItem)
    {
        resepItem.PropertyChanged += OnResepPropertyChanged;
    }

    private void AttachToppingHandler(ToppingModel topping)
    {
        topping.PropertyChanged += OnToppingPropertyChanged;
    }

    private void AttachProduksiSettingHandler(ProduksiSettingModel setting)
    {
        setting.PropertyChanged += OnProduksiSettingPropertyChanged;
    }

    private void DetachBahanHandler(BahanModel bahan)
    {
        bahan.PropertyChanged -= OnBahanPropertyChanged;
    }

    private void DetachResepHandler(ResepItemModel resepItem)
    {
        resepItem.PropertyChanged -= OnResepPropertyChanged;
    }

    private void DetachToppingHandler(ToppingModel topping)
    {
        topping.PropertyChanged -= OnToppingPropertyChanged;
    }

    private void DetachAllHandlers()
    {
        foreach (var bahan in BahanItems)
        {
            DetachBahanHandler(bahan);
        }

        foreach (var resep in ResepItems)
        {
            DetachResepHandler(resep);
        }

        foreach (var topping in ToppingItems)
        {
            DetachToppingHandler(topping);
        }

        if (ProduksiSetting is not null)
        {
            ProduksiSetting.PropertyChanged -= OnProduksiSettingPropertyChanged;
        }
    }

    private async void OnBahanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || sender is not BahanModel bahan)
        {
            return;
        }

        if (e.PropertyName is not (nameof(BahanModel.NamaBahan) or nameof(BahanModel.NettoPerPack) or nameof(BahanModel.HargaPerPack)))
        {
            return;
        }

        await SafeExecuteAsync(() => _bahanRepository.UpdateAsync(bahan));

        var resepItem = ResepItems.FirstOrDefault(item => item.BahanId == bahan.Id);
        if (resepItem is not null)
        {
            resepItem.NamaBahan = bahan.NamaBahan;
            resepItem.NettoPerPack = bahan.NettoPerPack;
            resepItem.HargaPerPack = bahan.HargaPerPack;
        }

        Recalculate();
    }

    private async void OnResepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || sender is not ResepItemModel resep)
        {
            return;
        }

        if (e.PropertyName != nameof(ResepItemModel.JumlahDipakai))
        {
            return;
        }

        await SafeExecuteAsync(() => _resepRepository.UpdateJumlahDipakaiAsync(resep.Id, resep.JumlahDipakai));
        Recalculate();
    }

    private async void OnToppingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || sender is not ToppingModel topping)
        {
            return;
        }

        if (e.PropertyName is not (nameof(ToppingModel.NamaTopping) or nameof(ToppingModel.BiayaPerDonat) or nameof(ToppingModel.IsActive)))
        {
            return;
        }

        await SafeExecuteAsync(() => _toppingRepository.UpdateAsync(topping));
        Recalculate();
    }

    private async void OnProduksiSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || sender is not ProduksiSettingModel setting)
        {
            return;
        }

        if (e.PropertyName is not (nameof(ProduksiSettingModel.JumlahDonatDihasilkan)
            or nameof(ProduksiSettingModel.WastePersen)
            or nameof(ProduksiSettingModel.TargetProfitPersen)
            or nameof(ProduksiSettingModel.HariProduksiPerBulan)))
        {
            return;
        }

        await SafeExecuteAsync(() => _produksiSettingRepository.UpdateAsync(setting));
        Recalculate();
    }

    private void Recalculate()
    {
        // Hitung modal per bahan dulu agar grid resep menampilkan biaya realtime per baris.
        foreach (var resepItem in ResepItems)
        {
            resepItem.ModalBahan = _costCalculationService.HitungModalBahan(
                resepItem.JumlahDipakai,
                resepItem.NettoPerPack,
                resepItem.HargaPerPack);
        }

        var totalModalAdonan = _costCalculationService.HitungTotalModalAdonan(ResepItems);
        var hppDonat = _costCalculationService.HitungHppDonat(totalModalAdonan, ProduksiSetting.JumlahDonatDihasilkan);
        var totalTopping = _costCalculationService.HitungTotalTopping(ToppingItems);
        var hppFinal = _costCalculationService.HitungHppFinal(hppDonat, totalTopping);

        var produksiEfektif = _productionCalculationService.HitungProduksiEfektif(
            ProduksiSetting.JumlahDonatDihasilkan,
            ProduksiSetting.WastePersen);
        var hppSetelahWaste = _productionCalculationService.HitungHppSetelahWaste(totalModalAdonan, produksiEfektif);

        var hargaJual = _profitService.HitungHargaJual(hppFinal, ProduksiSetting.TargetProfitPersen);
        var profitPerDonat = _profitService.HitungProfitPerDonat(hargaJual, hppFinal);
        var totalProfit = _profitService.HitungTotalProfit(profitPerDonat, produksiEfektif);
        var estimasiBulanan = _profitService.HitungEstimasiBulanan(totalProfit, ProduksiSetting.HariProduksiPerBulan);

        // Semua output kalkulasi disimpan dalam satu model supaya seluruh halaman bisa binding ke sumber yang sama.
        Calculation.TotalModalAdonan = totalModalAdonan;
        Calculation.HppDonat = hppDonat;
        Calculation.TotalTopping = totalTopping;
        Calculation.HppFinal = hppFinal;
        Calculation.ProduksiEfektif = produksiEfektif;
        Calculation.HppSetelahWaste = hppSetelahWaste;
        Calculation.HargaJual = hargaJual;
        Calculation.ProfitPerDonat = profitPerDonat;
        Calculation.TotalProfit = totalProfit;
        Calculation.EstimasiHarian = totalProfit;
        Calculation.EstimasiBulanan = estimasiBulanan;
    }

    private static async Task SafeExecuteAsync(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }
}

