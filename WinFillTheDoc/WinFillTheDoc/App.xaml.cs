using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Application.ViewModels;
using WinFillTheDoc.Domain.Placeholders;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IPlaceholderCatalog, PlaceholderRegistry>();
        services.AddSingleton<DocumentWorkflowState>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DocumentSetupViewModel>();
        services.AddTransient<DocumentDataFormViewModel>();
        services.AddTransient<ConfirmationViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var navigation = _serviceProvider.GetRequiredService<INavigationService>();
        navigation.Initialize(viewModel => mainWindowViewModel.CurrentViewModel = viewModel);
        navigation.NavigateTo<DocumentSetupViewModel>();

        var window = new MainWindow { DataContext = mainWindowViewModel };
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
