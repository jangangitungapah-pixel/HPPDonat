using System;
using HPPDonat.ViewModels;
using ReactiveUI;

namespace HPPDonat.Services;

public interface INavigationService
{
    ViewModelBase CurrentViewModel { get; }
    IObservable<ViewModelBase> CurrentViewModelChanged { get; }
    void NavigateTo<T>() where T : ViewModelBase;
    void NavigateTo(Type viewModelType);
    void NavigateTo(ViewModelBase viewModel);
}
