using WinFillTheDoc.Application.ViewModels;

namespace WinFillTheDoc.Application.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Action<ObservableObject>? _setCurrentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Action<ObservableObject> setCurrentViewModel)
    {
        _setCurrentViewModel = setCurrentViewModel;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        if (_setCurrentViewModel is null)
        {
            throw new InvalidOperationException("Navigation service is not initialized.");
        }

        _setCurrentViewModel((TViewModel)_serviceProvider.GetService(typeof(TViewModel))!);
    }
}
