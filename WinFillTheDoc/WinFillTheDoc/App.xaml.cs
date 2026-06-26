using System.Windows;
using FillTheDoc.DaDataClient;
using FillTheDoc.OpenAIClient;
using FillTheDoc.OpenAIClient.Abstractions;
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
        services.AddSingleton<IDocxTemplateService, DocxTemplateService>();
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
        services.AddSingleton<IApiKeyStore, JsonFileApiKeyStore>();
        services.AddSingleton<IDaDataTokenStore>(serviceProvider => (JsonFileApiKeyStore)serviceProvider.GetRequiredService<IApiKeyStore>());
        services.AddSingleton<IRequisitesExtractionService, OpenAIRequisitesExtractionService>();
        services.AddOpenAIClient(
            options => options.Model = "gpt-4o-mini",
            serviceProvider => new OpenAIApiKeyProvider(serviceProvider.GetRequiredService<IApiKeyStore>()));
        services.AddSingleton<ICompanyReferenceService, DaDataCompanyReferenceService>();
        services.AddSingleton<ICompanyReferenceValidator, CompanyReferenceValidator>();
        services.AddDaDataClient(
            _ => { },
            serviceProvider => new DaDataTokenProvider(serviceProvider.GetRequiredService<IDaDataTokenStore>()));
        services.AddSingleton<ICustomPlaceholderRepository, JsonCustomPlaceholderRepository>();
        services.AddSingleton<CustomPlaceholderValidator>();
        services.AddSingleton<IDocumentDataCopyStringBuilder, DocumentDataCopyStringBuilder>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IPlaceholderCatalog, PlaceholderRegistry>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<PlaceholderValueAssembler>();
        services.AddSingleton<DocumentWorkflowState>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DocumentSetupViewModel>();
        services.AddTransient<DocumentDataFormViewModel>();
        services.AddTransient<ConfirmationViewModel>();
        services.AddTransient<PlaceholderLibraryViewModel>();

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
