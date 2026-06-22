namespace WinFillTheDoc.Application.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private ObservableObject? _currentViewModel;

    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }
}
