using HPPDonat.Models;
using HPPDonat.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace HPPDonat.ViewModels;

public sealed class ToppingViewModel : ViewModelBase
{
    private readonly IAppStateService _appStateService;
    private ToppingModel? _selectedTopping;

    public ToppingViewModel(IAppStateService appStateService)
    {
        _appStateService = appStateService;

        TambahToppingCommand = ReactiveCommand.CreateFromTask(() => _appStateService.TambahToppingAsync());

        var canDelete = this
            .WhenAnyValue(viewModel => viewModel.SelectedTopping)
            .Select(topping => topping is not null);

        HapusToppingCommand = ReactiveCommand.CreateFromTask(
            () => _appStateService.HapusToppingAsync(SelectedTopping),
            canDelete);
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

