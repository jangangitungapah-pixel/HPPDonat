using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;
using HPPDonat.Data;
using HPPDonat.Helpers;
using HPPDonat.Models;
using ReactiveUI;

namespace HPPDonat.Services;

public sealed class AppStateService : ReactiveObject, IAppStateService, IDisposable
{
    private readonly IBahanRepository _bahanRepository;
    private readonly IResepRepository _resepRepository;
    private readonly IResepVarianRepository _resepVarianRepository;
    private readonly IToppingRepository _toppingRepository;
    private readonly IProduksiSettingRepository _produksiSettingRepository;
    private readonly IProductionDataCoordinator _productionDataCoordinator;
    private readonly ICostCalculationService _costCalculationService;
    private readonly IProductionCalculationService _productionCalculationService;
    private readonly IProfitService _profitService;

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly object _saveTokenLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _saveDebounceTokens = new();

    private bool _isInitializing;
    private bool _isNormalizing;
    private bool _isDisposed;
    private int _pendingWrites;
    private ProduksiSettingModel _produksiSetting = new();
    private ResepVarianModel? _varianAktif;

    public AppStateService(
        IBahanRepository bahanRepository,
        IResepRepository resepRepository,
        IResepVarianRepository resepVarianRepository,
        IToppingRepository toppingRepository,
        IProduksiSettingRepository produksiSettingRepository,
        IProductionDataCoordinator productionDataCoordinator,
        ICostCalculationService costCalculationService,
        IProductionCalculationService productionCalculationService,
        IProfitService profitService)
    {
        _bahanRepository = bahanRepository;
        _resepRepository = resepRepository;
        _resepVarianRepository = resepVarianRepository;
        _toppingRepository = toppingRepository;
        _produksiSettingRepository = produksiSettingRepository;
        _productionDataCoordinator = productionDataCoordinator;
        _costCalculationService = costCalculationService;
        _productionCalculationService = productionCalculationService;
        _profitService = profitService;

        BahanItems = new ObservableCollection<BahanModel>();
        ResepItems = new ObservableCollection<ResepItemModel>();
        ResepVarianItems = new ObservableCollection<ResepVarianModel>();
        ToppingItems = new ObservableCollection<ToppingModel>();
        Calculation = new CalculationOutputModel();
        Status = new AppStatusModel();
    }

    public ObservableCollection<BahanModel> BahanItems { get; }

    public ObservableCollection<ResepItemModel> ResepItems { get; }

    public ObservableCollection<ResepVarianModel> ResepVarianItems { get; }

    public ObservableCollection<ToppingModel> ToppingItems { get; }

    public ProduksiSettingModel ProduksiSetting
    {
        get => _produksiSetting;
        private set => this.RaiseAndSetIfChanged(ref _produksiSetting, value);
    }

    public ResepVarianModel? VarianAktif
    {
        get => _varianAktif;
        private set => this.RaiseAndSetIfChanged(ref _varianAktif, value);
    }

    public CalculationOutputModel Calculation { get; }

    public AppStatusModel Status { get; }

    public async Task InitializeAsync()
    {
        _isInitializing = true;

        DetachAllHandlers();
        CancelAllScheduledSaves();

        BahanItems.Clear();
        ResepItems.Clear();
        ResepVarianItems.Clear();
        ToppingItems.Clear();

        var bahanList = await _bahanRepository.GetAllAsync();
        foreach (var bahan in bahanList)
        {
            NormalizeBahan(bahan);
            BahanItems.Add(bahan);
            AttachBahanHandler(bahan);
        }

        var varianList = await _resepVarianRepository.GetAllAsync();
        if (varianList.Count == 0)
        {
            var defaultVarian = await _resepVarianRepository.AddAsync("Default", true);
            varianList = new List<ResepVarianModel> { defaultVarian };
        }

        foreach (var varian in varianList)
        {
            ResepVarianItems.Add(varian);
        }

        var activeVarian = ResepVarianItems.FirstOrDefault(v => v.IsActive) ?? ResepVarianItems.First();
        if (!activeVarian.IsActive)
        {
            await _resepVarianRepository.SetActiveAsync(activeVarian.Id);
            foreach (var varian in ResepVarianItems)
            {
                varian.IsActive = varian.Id == activeVarian.Id;
            }
        }

        VarianAktif = activeVarian;
        await LoadResepItemsUntukVarianAsync(activeVarian.Id);

        var toppingList = await _toppingRepository.GetAllAsync();
        foreach (var topping in toppingList)
        {
            NormalizeTopping(topping);
            ToppingItems.Add(topping);
            AttachToppingHandler(topping);
        }

        ProduksiSetting = await _produksiSettingRepository.GetOrCreateAsync();
        NormalizeProduksiSetting(ProduksiSetting);
        AttachProduksiSettingHandler(ProduksiSetting);

        _isInitializing = false;
        Recalculate();
        SetStatusSuccess("Data berhasil dimuat.");
    }

    public async Task PilihVarianResepAsync(ResepVarianModel? varian)
    {
        if (varian is null || VarianAktif?.Id == varian.Id)
        {
            return;
        }

        await ExecuteImmediateWriteAsync(async () =>
        {
            await _resepVarianRepository.SetActiveAsync(varian.Id);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in ResepVarianItems)
                {
                    item.IsActive = item.Id == varian.Id;
                }

                VarianAktif = ResepVarianItems.First(v => v.Id == varian.Id);
            });

            await LoadResepItemsUntukVarianAsync(varian.Id);
            await Dispatcher.UIThread.InvokeAsync(Recalculate);
        }, "Varian resep aktif berhasil diganti.");
    }

    public async Task TambahVarianResepAsync(string? namaVarian = null, bool duplikatDariVarianAktif = false)
    {
        await ExecuteImmediateWriteAsync(async () =>
        {
            var namaFinal = BuatNamaVarianUnik(InputValidator.NormalizeName(namaVarian, "Varian Baru"));
            var varianBaru = await _resepVarianRepository.AddAsync(namaFinal, false);

            var sumber = duplikatDariVarianAktif
                ? ResepItems.ToDictionary(item => item.BahanId, item => item.JumlahDipakai)
                : new Dictionary<int, decimal>();

            foreach (var bahan in BahanItems)
            {
                var jumlah = sumber.TryGetValue(bahan.Id, out var value) ? value : 0m;
                await _resepRepository.AddAsync(new ResepModel
                {
                    BahanId = bahan.Id,
                    VarianId = varianBaru.Id,
                    JumlahDipakai = jumlah
                });
            }

            await _resepVarianRepository.SetActiveAsync(varianBaru.Id);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in ResepVarianItems)
                {
                    item.IsActive = false;
                }

                varianBaru.IsActive = true;
                ResepVarianItems.Add(varianBaru);
                VarianAktif = varianBaru;
            });

            await LoadResepItemsUntukVarianAsync(varianBaru.Id);
            await Dispatcher.UIThread.InvokeAsync(Recalculate);
        }, duplikatDariVarianAktif
            ? "Varian resep berhasil diduplikasi."
            : "Varian resep baru berhasil ditambahkan.");
    }

    public Task DuplikasiVarianAktifAsync()
    {
        var nama = VarianAktif is null
            ? "Varian Copy"
            : $"{VarianAktif.NamaVarian} Copy";

        return TambahVarianResepAsync(nama, true);
    }

    public async Task HapusVarianResepAsync(ResepVarianModel? varian)
    {
        if (varian is null)
        {
            return;
        }

        if (ResepVarianItems.Count <= 1)
        {
            SetStatusError("Minimal harus ada satu varian resep aktif.");
            return;
        }

        await ExecuteImmediateWriteAsync(async () =>
        {
            var varianTarget = ResepVarianItems.FirstOrDefault(v => v.Id != varian.Id);

            await _resepVarianRepository.DeleteAsync(varian.Id);

            if (varianTarget is not null)
            {
                await _resepVarianRepository.SetActiveAsync(varianTarget.Id);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ResepVarianItems.Remove(varian);

                if (varianTarget is not null)
                {
                    foreach (var item in ResepVarianItems)
                    {
                        item.IsActive = item.Id == varianTarget.Id;
                    }

                    VarianAktif = varianTarget;
                }
            });

            if (varianTarget is not null)
            {
                await LoadResepItemsUntukVarianAsync(varianTarget.Id);
                await Dispatcher.UIThread.InvokeAsync(Recalculate);
            }
        }, "Varian resep berhasil dihapus.");
    }

    public async Task ResetResepAktifAsync()
    {
        if (VarianAktif is null)
        {
            return;
        }

        await ExecuteImmediateWriteAsync(async () =>
        {
            await _resepRepository.ResetByVarianAsync(VarianAktif.Id);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isInitializing = true;
                foreach (var item in ResepItems)
                {
                    item.JumlahDipakai = 0m;
                }

                _isInitializing = false;
                Recalculate();
            });
        }, "Resep aktif berhasil direset.");
    }

    public async Task TambahBahanAsync()
    {
        var bahanBaru = new BahanModel
        {
            NamaBahan = "Bahan Baru",
            Satuan = "gram",
            NettoPerPack = 1000m,
            HargaPerPack = 0m
        };

        await ExecuteImmediateWriteAsync(async () =>
        {
            var varianIds = ResepVarianItems.Select(v => v.Id).ToList();
            var result = await _productionDataCoordinator.AddBahanDanResepAsync(bahanBaru, varianIds, 0m);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BahanItems.Add(result.bahan);
                AttachBahanHandler(result.bahan);

                if (VarianAktif is not null)
                {
                    var resepAktif = result.resepItems.FirstOrDefault(item => item.VarianId == VarianAktif.Id);
                    if (resepAktif is not null)
                    {
                        var resepItem = BuatResepItem(result.bahan, resepAktif);
                        ResepItems.Add(resepItem);
                        AttachResepHandler(resepItem);
                    }
                }

                Recalculate();
            });
        }, "Bahan baru berhasil ditambahkan.");
    }

    public async Task HapusBahanAsync(BahanModel? bahan)
    {
        if (bahan is null)
        {
            return;
        }

        await ExecuteImmediateWriteAsync(async () =>
        {
            await _productionDataCoordinator.DeleteBahanDanResepAsync(bahan.Id);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DetachBahanHandler(bahan);
                BahanItems.Remove(bahan);

                var resepItem = ResepItems.FirstOrDefault(item => item.BahanId == bahan.Id);
                if (resepItem is not null)
                {
                    DetachResepHandler(resepItem);
                    ResepItems.Remove(resepItem);
                }

                Recalculate();
            });
        }, "Bahan berhasil dihapus.");
    }

    public async Task TambahToppingAsync()
    {
        var toppingBaru = new ToppingModel
        {
            NamaTopping = "Topping Baru",
            BiayaPerDonat = 0m,
            IsActive = true
        };

        await ExecuteImmediateWriteAsync(async () =>
        {
            var saved = await _toppingRepository.AddAsync(toppingBaru);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToppingItems.Add(saved);
                AttachToppingHandler(saved);
                Recalculate();
            });
        }, "Topping baru berhasil ditambahkan.");
    }

    public async Task HapusToppingAsync(ToppingModel? topping)
    {
        if (topping is null)
        {
            return;
        }

        await ExecuteImmediateWriteAsync(async () =>
        {
            await _toppingRepository.DeleteAsync(topping.Id);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DetachToppingHandler(topping);
                ToppingItems.Remove(topping);
                Recalculate();
            });
        }, "Topping berhasil dihapus.");
    }

    public void ClearErrorStatus()
    {
        if (Status.IsError)
        {
            SetStatusSuccess("Siap digunakan.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        DetachAllHandlers();
        CancelAllScheduledSaves();
        _writeLock.Dispose();
    }

    private async Task LoadResepItemsUntukVarianAsync(int varianId)
    {
        _isInitializing = true;

        foreach (var item in ResepItems)
        {
            DetachResepHandler(item);
        }

        ResepItems.Clear();

        var resepList = await _resepRepository.GetByVarianAsync(varianId);
        var resepMap = resepList.ToDictionary(resep => resep.BahanId);

        foreach (var bahan in BahanItems)
        {
            if (!resepMap.TryGetValue(bahan.Id, out var resep))
            {
                resep = await _resepRepository.AddAsync(new ResepModel
                {
                    BahanId = bahan.Id,
                    VarianId = varianId,
                    JumlahDipakai = 0m
                });
            }

            var resepItem = BuatResepItem(bahan, resep);
            ResepItems.Add(resepItem);
            AttachResepHandler(resepItem);
        }

        _isInitializing = false;
    }

    private ResepItemModel BuatResepItem(BahanModel bahan, ResepModel resep)
    {
        return new ResepItemModel
        {
            Id = resep.Id,
            BahanId = bahan.Id,
            NamaBahan = bahan.NamaBahan,
            Satuan = bahan.Satuan,
            NettoPerPack = bahan.NettoPerPack,
            HargaPerPack = bahan.HargaPerPack,
            JumlahDipakai = InputValidator.ClampNonNegative(resep.JumlahDipakai)
        };
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

    private void CancelAllScheduledSaves()
    {
        lock (_saveTokenLock)
        {
            foreach (var token in _saveDebounceTokens.Values)
            {
                token.Cancel();
                token.Dispose();
            }

            _saveDebounceTokens.Clear();
        }
    }

    private void OnBahanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || _isNormalizing || sender is not BahanModel bahan)
        {
            return;
        }

        if (e.PropertyName is not (nameof(BahanModel.NamaBahan) or nameof(BahanModel.Satuan) or nameof(BahanModel.NettoPerPack) or nameof(BahanModel.HargaPerPack)))
        {
            return;
        }

        if (NormalizeBahan(bahan))
        {
            return;
        }

        var resepItem = ResepItems.FirstOrDefault(item => item.BahanId == bahan.Id);
        if (resepItem is not null)
        {
            resepItem.NamaBahan = bahan.NamaBahan;
            resepItem.Satuan = bahan.Satuan;
            resepItem.NettoPerPack = bahan.NettoPerPack;
            resepItem.HargaPerPack = bahan.HargaPerPack;
        }

        Recalculate();
        ScheduleSave($"bahan:{bahan.Id}", () => _bahanRepository.UpdateAsync(bahan), "Perubahan bahan tersimpan.");
    }

    private void OnResepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || _isNormalizing || sender is not ResepItemModel resep)
        {
            return;
        }

        if (e.PropertyName != nameof(ResepItemModel.JumlahDipakai))
        {
            return;
        }

        if (NormalizeResep(resep))
        {
            return;
        }

        Recalculate();
        ScheduleSave($"resep:{resep.Id}", () => _resepRepository.UpdateJumlahDipakaiAsync(resep.Id, resep.JumlahDipakai), "Perubahan resep tersimpan.");
    }

    private void OnToppingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || _isNormalizing || sender is not ToppingModel topping)
        {
            return;
        }

        if (e.PropertyName is not (nameof(ToppingModel.NamaTopping) or nameof(ToppingModel.BiayaPerDonat) or nameof(ToppingModel.IsActive)))
        {
            return;
        }

        if (NormalizeTopping(topping))
        {
            return;
        }

        Recalculate();
        ScheduleSave($"topping:{topping.Id}", () => _toppingRepository.UpdateAsync(topping), "Perubahan topping tersimpan.");
    }

    private void OnProduksiSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInitializing || _isNormalizing || sender is not ProduksiSettingModel setting)
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

        if (NormalizeProduksiSetting(setting))
        {
            return;
        }

        Recalculate();
        ScheduleSave("produksi-setting", () => _produksiSettingRepository.UpdateAsync(setting), "Pengaturan produksi tersimpan.");
    }

    private void ScheduleSave(string key, Func<Task> action, string successMessage)
    {
        if (_isDisposed)
        {
            return;
        }

        CancellationTokenSource cts;

        lock (_saveTokenLock)
        {
            if (_saveDebounceTokens.TryGetValue(key, out var existingToken))
            {
                existingToken.Cancel();
                existingToken.Dispose();
            }

            cts = new CancellationTokenSource();
            _saveDebounceTokens[key] = cts;
        }

        Interlocked.Increment(ref _pendingWrites);
        SetStatusBusy("Menyimpan perubahan...");

        _ = PersistDebouncedAsync(key, cts, action, successMessage);
    }

    private async Task PersistDebouncedAsync(string key, CancellationTokenSource cts, Func<Task> action, string successMessage)
    {
        try
        {
            await Task.Delay(250, cts.Token);
            await _writeLock.WaitAsync(cts.Token);

            try
            {
                await action();
                SetStatusSuccess(successMessage);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            SetStatusError($"Gagal menyimpan data: {exception.Message}");
        }
        finally
        {
            lock (_saveTokenLock)
            {
                if (_saveDebounceTokens.TryGetValue(key, out var currentToken) && ReferenceEquals(currentToken, cts))
                {
                    _saveDebounceTokens.Remove(key);
                }
            }

            cts.Dispose();

            if (Interlocked.Decrement(ref _pendingWrites) == 0 && !Status.IsError)
            {
                SetStatusSuccess("Semua perubahan tersimpan.");
            }
        }
    }

    private async Task ExecuteImmediateWriteAsync(Func<Task> action, string successMessage)
    {
        Interlocked.Increment(ref _pendingWrites);
        SetStatusBusy("Menyimpan perubahan...");

        try
        {
            await _writeLock.WaitAsync();
            try
            {
                await action();
                SetStatusSuccess(successMessage);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            SetStatusError($"Operasi gagal: {exception.Message}");
        }
        finally
        {
            if (Interlocked.Decrement(ref _pendingWrites) == 0 && !Status.IsError)
            {
                SetStatusSuccess("Semua perubahan tersimpan.");
            }
        }
    }

    private bool NormalizeBahan(BahanModel bahan)
    {
        var newNama = InputValidator.NormalizeName(bahan.NamaBahan, "Bahan");
        var newSatuan = InputValidator.NormalizeSatuan(bahan.Satuan);
        var newNetto = InputValidator.ClampPositive(bahan.NettoPerPack, 1m);
        var newHarga = InputValidator.ClampNonNegative(bahan.HargaPerPack);

        if (newNama == bahan.NamaBahan && newSatuan == bahan.Satuan && newNetto == bahan.NettoPerPack && newHarga == bahan.HargaPerPack)
        {
            return false;
        }

        _isNormalizing = true;
        bahan.NamaBahan = newNama;
        bahan.Satuan = newSatuan;
        bahan.NettoPerPack = newNetto;
        bahan.HargaPerPack = newHarga;
        _isNormalizing = false;
        return true;
    }

    private bool NormalizeResep(ResepItemModel resep)
    {
        var newJumlah = InputValidator.ClampNonNegative(resep.JumlahDipakai);
        if (newJumlah == resep.JumlahDipakai)
        {
            return false;
        }

        _isNormalizing = true;
        resep.JumlahDipakai = newJumlah;
        _isNormalizing = false;
        return true;
    }

    private bool NormalizeTopping(ToppingModel topping)
    {
        var newNama = InputValidator.NormalizeName(topping.NamaTopping, "Topping");
        var newBiaya = InputValidator.ClampNonNegative(topping.BiayaPerDonat);

        if (newNama == topping.NamaTopping && newBiaya == topping.BiayaPerDonat)
        {
            return false;
        }

        _isNormalizing = true;
        topping.NamaTopping = newNama;
        topping.BiayaPerDonat = newBiaya;
        _isNormalizing = false;
        return true;
    }

    private bool NormalizeProduksiSetting(ProduksiSettingModel setting)
    {
        var newJumlah = InputValidator.ClampPositive(setting.JumlahDonatDihasilkan, 1m);
        var newWaste = InputValidator.Clamp(setting.WastePersen, 0m, 99m);
        var newProfit = InputValidator.Clamp(setting.TargetProfitPersen, 1m, 95m);
        var newHari = InputValidator.Clamp(setting.HariProduksiPerBulan, 1, 31);

        if (newJumlah == setting.JumlahDonatDihasilkan
            && newWaste == setting.WastePersen
            && newProfit == setting.TargetProfitPersen
            && newHari == setting.HariProduksiPerBulan)
        {
            return false;
        }

        _isNormalizing = true;
        setting.JumlahDonatDihasilkan = newJumlah;
        setting.WastePersen = newWaste;
        setting.TargetProfitPersen = newProfit;
        setting.HariProduksiPerBulan = newHari;
        _isNormalizing = false;
        return true;
    }

    private void Recalculate()
    {
        foreach (var resepItem in ResepItems)
        {
            resepItem.ModalBahan = _costCalculationService.HitungModalBahan(
                resepItem.JumlahDipakai,
                resepItem.NettoPerPack,
                resepItem.HargaPerPack);

            resepItem.ValidationMessage = ValidateResepItem(resepItem);
        }

        var totalModalAdonan = _costCalculationService.HitungTotalModalAdonan(ResepItems);

        foreach (var resepItem in ResepItems)
        {
            resepItem.KontribusiPersen = totalModalAdonan > 0m
                ? (resepItem.ModalBahan / totalModalAdonan) * 100m
                : 0m;
        }

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

    private static string ValidateResepItem(ResepItemModel item)
    {
        if (item.NettoPerPack <= 0m)
        {
            return "Netto tidak valid";
        }

        if (item.HargaPerPack < 0m)
        {
            return "Harga tidak valid";
        }

        if (item.JumlahDipakai <= 0m)
        {
            return "Belum dipakai";
        }

        return "OK";
    }

    private string BuatNamaVarianUnik(string baseName)
    {
        var nama = baseName;
        var counter = 2;

        while (ResepVarianItems.Any(v => string.Equals(v.NamaVarian, nama, StringComparison.OrdinalIgnoreCase)))
        {
            nama = $"{baseName} ({counter})";
            counter++;
        }

        return nama;
    }

    private void SetStatusBusy(string message)
    {
        UpdateStatus(() =>
        {
            Status.IsBusy = true;
            Status.IsError = false;
            Status.Message = message;
            Status.LastUpdated = DateTimeOffset.Now;
        });
    }

    private void SetStatusSuccess(string message)
    {
        UpdateStatus(() =>
        {
            Status.IsBusy = false;
            Status.IsError = false;
            Status.Message = message;
            Status.LastUpdated = DateTimeOffset.Now;
        });
    }

    private void SetStatusError(string message)
    {
        UpdateStatus(() =>
        {
            Status.IsBusy = false;
            Status.IsError = true;
            Status.Message = message;
            Status.LastUpdated = DateTimeOffset.Now;
        });
    }

    private static void UpdateStatus(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}
