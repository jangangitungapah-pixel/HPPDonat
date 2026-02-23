using ReactiveUI;

namespace HPPDonat.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    public virtual string Title { get; } = "HPP Donat Calculator";
}

