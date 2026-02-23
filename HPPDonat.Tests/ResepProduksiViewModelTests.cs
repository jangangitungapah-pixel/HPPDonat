using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using HPPDonat.Models;
using HPPDonat.Services;
using HPPDonat.ViewModels;

namespace HPPDonat.Tests;

public sealed class ResepProduksiViewModelTests
{
    [Fact]
    public async Task TambahVarianCommand_MenyelaraskanSelectedVarianDenganVarianAktif()
    {
        var appState = new FakeAppStateService();
        var viewModel = new ResepProduksiViewModel(appState, new ConfirmAlwaysTrueDialogService());

        await viewModel.TambahVarianCommand.Execute();

        Assert.NotNull(viewModel.SelectedVarian);
        Assert.NotNull(appState.VarianAktif);
        Assert.Equal(appState.VarianAktif!.Id, viewModel.SelectedVarian!.Id);
        Assert.Equal(2, appState.ResepVarianItems.Count);
    }

    [Fact]
    public async Task HapusVarianCommand_TidakAktifSaatHanyaSatuVarian()
    {
        var appState = new FakeAppStateService();
        var viewModel = new ResepProduksiViewModel(appState, new ConfirmAlwaysTrueDialogService());

        var canExecute = await viewModel.HapusVarianCommand.CanExecute.FirstAsync();

        Assert.False(canExecute);
    }

    [Fact]
    public async Task SelectedVarian_MenggantiVarianAktifDiAppState()
    {
        var appState = new FakeAppStateService();
        await appState.TambahVarianResepAsync("Varian 2", false);

        var viewModel = new ResepProduksiViewModel(appState, new ConfirmAlwaysTrueDialogService());
        var target = appState.ResepVarianItems.First(item => item.Id != appState.VarianAktif!.Id);

        viewModel.SelectedVarian = target;
        await Task.Delay(20);

        Assert.NotNull(appState.VarianAktif);
        Assert.Equal(target.Id, appState.VarianAktif!.Id);
        Assert.Equal(target.Id, viewModel.SelectedVarian!.Id);
    }

    private sealed class ConfirmAlwaysTrueDialogService : IUserDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeAppStateService : IAppStateService
    {
        private int _nextVarianId = 2;
        private ResepVarianModel? _varianAktif;

        public FakeAppStateService()
        {
            var defaultVarian = new ResepVarianModel
            {
                Id = 1,
                NamaVarian = "Default",
                IsActive = true
            };

            ResepVarianItems.Add(defaultVarian);
            _varianAktif = defaultVarian;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<BahanModel> BahanItems { get; } = new();

        public ObservableCollection<ResepItemModel> ResepItems { get; } = new();

        public ObservableCollection<ResepVarianModel> ResepVarianItems { get; } = new();

        public ObservableCollection<ToppingModel> ToppingItems { get; } = new();

        public ProduksiSettingModel ProduksiSetting { get; } = new();

        public CalculationOutputModel Calculation { get; } = new();

        public AppStatusModel Status { get; } = new();

        public ResepVarianModel? VarianAktif
        {
            get => _varianAktif;
            private set
            {
                if (_varianAktif?.Id == value?.Id)
                {
                    return;
                }

                _varianAktif = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VarianAktif)));
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task TambahBahanAsync()
        {
            return Task.CompletedTask;
        }

        public Task HapusBahanAsync(BahanModel? bahan)
        {
            return Task.CompletedTask;
        }

        public Task TambahToppingAsync()
        {
            return Task.CompletedTask;
        }

        public Task HapusToppingAsync(ToppingModel? topping)
        {
            return Task.CompletedTask;
        }

        public Task PilihVarianResepAsync(ResepVarianModel? varian)
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
            foreach (var item in ResepVarianItems)
            {
                item.IsActive = false;
            }

            var baru = new ResepVarianModel
            {
                Id = _nextVarianId++,
                NamaVarian = namaVarian ?? $"Varian {_nextVarianId}",
                IsActive = true
            };

            ResepVarianItems.Add(baru);
            VarianAktif = baru;
            return Task.CompletedTask;
        }

        public Task DuplikasiVarianAktifAsync()
        {
            var nama = VarianAktif is null ? "Varian Copy" : $"{VarianAktif.NamaVarian} Copy";
            return TambahVarianResepAsync(nama, true);
        }

        public Task HapusVarianResepAsync(ResepVarianModel? varian)
        {
            if (varian is null || ResepVarianItems.Count <= 1)
            {
                return Task.CompletedTask;
            }

            ResepVarianItems.Remove(varian);

            var next = ResepVarianItems.First();
            next.IsActive = true;
            VarianAktif = next;
            return Task.CompletedTask;
        }

        public Task ResetResepAktifAsync()
        {
            return Task.CompletedTask;
        }

        public void ClearErrorStatus()
        {
        }
    }
}