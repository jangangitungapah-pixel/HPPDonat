using HPPDonat.Models;
using HPPDonat.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace HPPDonat.ViewModels;

public sealed class MasterBahanViewModel : ViewModelBase
{
    private readonly IAppStateService _appStateService;
    private BahanModel? _selectedBahan;

    public MasterBahanViewModel(IAppStateService appStateService)
    {
        _appStateService = appStateService;

        TambahBahanCommand = ReactiveCommand.CreateFromTask(() => _appStateService.TambahBahanAsync());

        var canDelete = this
            .WhenAnyValue(viewModel => viewModel.SelectedBahan)
            .Select(bahan => bahan is not null);

        HapusBahanCommand = ReactiveCommand.CreateFromTask(
            () => _appStateService.HapusBahanAsync(SelectedBahan),
            canDelete);
    }

    public ObservableCollection<BahanModel> BahanItems => _appStateService.BahanItems;

    public BahanModel? SelectedBahan
    {
        get => _selectedBahan;
        set => this.RaiseAndSetIfChanged(ref _selectedBahan, value);
    }

    public ReactiveCommand<Unit, Unit> TambahBahanCommand { get; }

    public ReactiveCommand<Unit, Unit> HapusBahanCommand { get; }
}

