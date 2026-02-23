using System;
using HPPDonat.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace HPPDonat.Services;

public class NavigationService : ReactiveObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public IObservable<ViewModelBase> CurrentViewModelChanged => 
        this.WhenAnyValue(x => x.CurrentViewModel);

    public void NavigateTo<T>() where T : ViewModelBase
    {
        NavigateTo(typeof(T));
    }

    public void NavigateTo(Type viewModelType)
    {
        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
        {
            throw new ArgumentException("Type must be a ViewModelBase", nameof(viewModelType));
        }

        var viewModel = (ViewModelBase)_serviceProvider.GetRequiredService(viewModelType);
        NavigateTo(viewModel);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
