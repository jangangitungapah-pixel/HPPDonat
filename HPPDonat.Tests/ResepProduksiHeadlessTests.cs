using Avalonia.Headless.XUnit;
using HPPDonat.ViewModels;
using HPPDonat.Views;

namespace HPPDonat.Tests;

public sealed class ResepProduksiHeadlessTests
{
    [AvaloniaFact]
    public void ResepProduksiView_DapatDirenderDalamWindow()
    {
        var appState = new FakeResepAppStateService();
        var viewModel = new ResepProduksiViewModel(appState, new ConfirmAlwaysTrueDialogService());

        var view = new ResepProduksiView
        {
            DataContext = viewModel
        };

        view.ApplyTemplate();
        Assert.NotNull(view);
        Assert.NotNull(view.DataContext);
        viewModel.Dispose();
    }

    private sealed class ConfirmAlwaysTrueDialogService : Services.IUserDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeResepAppStateService : Services.IAppStateService
    {
        private Models.ResepVarianModel? _varianAktif;

        public FakeResepAppStateService()
        {
            var defaultVarian = new Models.ResepVarianModel
            {
                Id = 1,
                NamaVarian = "Default",
                IsActive = true
            };

            ResepVarianItems.Add(defaultVarian);
            _varianAktif = defaultVarian;

            ProduksiSetting.JumlahDonatDihasilkan = 100;
            ProduksiSetting.WastePersen = 5;
            ProduksiSetting.TargetProfitPersen = 20;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public System.Collections.ObjectModel.ObservableCollection<Models.BahanModel> BahanItems { get; } = new();

        public System.Collections.ObjectModel.ObservableCollection<Models.ResepItemModel> ResepItems { get; } = new()
        {
            new Models.ResepItemModel
            {
                Id = 1,
                BahanId = 1,
                NamaBahan = "Tepung",
                Satuan = "gram",
                NettoPerPack = 1000,
                HargaPerPack = 15000,
                JumlahDipakai = 500,
                ModalBahan = 7500,
                KontribusiPersen = 100,
                ValidationMessage = "OK"
            }
        };

        public System.Collections.ObjectModel.ObservableCollection<Models.ResepVarianModel> ResepVarianItems { get; } = new();

        public System.Collections.ObjectModel.ObservableCollection<Models.ToppingModel> ToppingItems { get; } = new();

        public Models.ProduksiSettingModel ProduksiSetting { get; } = new();

        public Models.CalculationOutputModel Calculation { get; } = new()
        {
            TotalModalAdonan = 7500,
            HppDonat = 75,
            TotalTopping = 500,
            HppFinal = 575,
            ProduksiEfektif = 95,
            HppSetelahWaste = 78.95m
        };

        public Models.AppStatusModel Status { get; } = new();

        public Models.ResepVarianModel? VarianAktif
        {
            get => _varianAktif;
            private set
            {
                if (_varianAktif?.Id == value?.Id)
                {
                    return;
                }

                _varianAktif = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(VarianAktif)));
            }
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task TambahBahanAsync() => Task.CompletedTask;

        public Task HapusBahanAsync(Models.BahanModel? bahan) => Task.CompletedTask;

        public Task TambahToppingAsync() => Task.CompletedTask;

        public Task HapusToppingAsync(Models.ToppingModel? topping) => Task.CompletedTask;

        public Task PilihVarianResepAsync(Models.ResepVarianModel? varian)
        {
            if (varian is null)
            {
                return Task.CompletedTask;
            }

            foreach (var item in ResepVarianItems)
            {
                item.IsActive = item.Id == varian.Id;
            }

            VarianAktif = ResepVarianItems.FirstOrDefault(item => item.Id == varian.Id);
            return Task.CompletedTask;
        }

        public Task TambahVarianResepAsync(string? namaVarian = null, bool duplikatDariVarianAktif = false)
        {
            var varianBaru = new Models.ResepVarianModel
            {
                Id = ResepVarianItems.Count + 1,
                NamaVarian = namaVarian ?? "Varian Baru",
                IsActive = true
            };

            foreach (var item in ResepVarianItems)
            {
                item.IsActive = false;
            }

            ResepVarianItems.Add(varianBaru);
            VarianAktif = varianBaru;
            return Task.CompletedTask;
        }

        public Task DuplikasiVarianAktifAsync() => TambahVarianResepAsync("Copy", true);

        public Task HapusVarianResepAsync(Models.ResepVarianModel? varian)
        {
            if (varian is null || ResepVarianItems.Count <= 1)
            {
                return Task.CompletedTask;
            }

            ResepVarianItems.Remove(varian);
            VarianAktif = ResepVarianItems.FirstOrDefault();
            return Task.CompletedTask;
        }

        public Task ResetResepAktifAsync() => Task.CompletedTask;

        public void ClearErrorStatus()
        {
        }
    }
}
