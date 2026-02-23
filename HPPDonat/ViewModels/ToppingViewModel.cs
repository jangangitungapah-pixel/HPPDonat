using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using HPPDonat.Models;
using HPPDonat.Services;
using ReactiveUI;

namespace HPPDonat.ViewModels;

public sealed class ToppingViewModel : ViewModelBase
{
    public override string Title => "Manajemen Topping";

    private readonly IAppStateService _appStateService;
    private readonly IUserDialogService _dialogService;
    private ToppingModel? _selectedTopping;

    public ToppingViewModel(IAppStateService appStateService, IUserDialogService dialogService)
    {
        _appStateService = appStateService;
        _dialogService = dialogService;

        TambahToppingCommand = ReactiveCommand.CreateFromTask(() => _appStateService.TambahToppingAsync());

        var canDelete = this
            .WhenAnyValue(viewModel => viewModel.SelectedTopping)
            .Select(topping => topping is not null);

        HapusToppingCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedTopping is null)
            {
                return;
            }

            var disetujui = await _dialogService.ConfirmAsync(
                "Konfirmasi Hapus Topping",
                $"Yakin ingin menghapus topping '{SelectedTopping.NamaTopping}'?");

            if (!disetujui)
            {
                return;
            }

            await _appStateService.HapusToppingAsync(SelectedTopping);
        }, canDelete);
    }

    public ObservableCollection<ToppingModel> ToppingItems => _appStateService.ToppingItems;

    public ToppingModel? SelectedTopping
    {
        get => _selectedTopping;
        set => this.RaiseAndSetIfChanged(ref _selectedTopping, value);
    }

    public ReactiveCommand<Unit, Unit> TambahToppingCommand { get; }

    public ReactiveCommand<Unit, Unit> HapusToppingCommand { get; }

    public CalculationOutputModel Ringkasan => _appStateService.Calculation;
}
