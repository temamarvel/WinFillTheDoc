using WinFillTheDoc.Application.ViewModels;

namespace WinFillTheDoc.Application.Navigation;

public interface INavigationService
{
    void Initialize(Action<ObservableObject> setCurrentViewModel);
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}
