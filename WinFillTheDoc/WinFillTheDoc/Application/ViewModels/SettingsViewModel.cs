using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IApiKeyStore _apiKeyStore;
    private readonly IDaDataTokenStore _daDataTokenStore;
    private readonly INavigationService _navigationService;
    private string _openAiApiKey = string.Empty;
    private string _daDataToken = string.Empty;
    private string? _message;

    public SettingsViewModel(
        IApiKeyStore apiKeyStore,
        IDaDataTokenStore daDataTokenStore,
        INavigationService navigationService)
    {
        _apiKeyStore = apiKeyStore;
        _daDataTokenStore = daDataTokenStore;
        _navigationService = navigationService;

        SaveOpenAiCommand = new RelayCommand(SaveOpenAi, () => !string.IsNullOrWhiteSpace(OpenAiApiKey));
        DeleteOpenAiCommand = new RelayCommand(DeleteOpenAi, () => _apiKeyStore.HasApiKey);
        SaveDaDataCommand = new RelayCommand(SaveDaData, () => !string.IsNullOrWhiteSpace(DaDataToken));
        DeleteDaDataCommand = new RelayCommand(DeleteDaData, () => _daDataTokenStore.HasToken);
        BackCommand = new RelayCommand(() => _navigationService.NavigateTo<DocumentSetupViewModel>());
    }

    public string OpenAiApiKey
    {
        get => _openAiApiKey;
        set
        {
            if (!SetProperty(ref _openAiApiKey, value)) return;
            SaveOpenAiCommand.RaiseCanExecuteChanged();
        }
    }

    public string DaDataToken
    {
        get => _daDataToken;
        set
        {
            if (!SetProperty(ref _daDataToken, value)) return;
            SaveDaDataCommand.RaiseCanExecuteChanged();
        }
    }

    public string OpenAiStatus => _apiKeyStore.HasApiKey ? "OpenAI API-ключ задан." : "OpenAI API-ключ не задан.";
    public string DaDataStatus => _daDataTokenStore.HasToken ? "DaData API-ключ задан." : "DaData API-ключ не задан.";

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public RelayCommand SaveOpenAiCommand { get; }
    public RelayCommand DeleteOpenAiCommand { get; }
    public RelayCommand SaveDaDataCommand { get; }
    public RelayCommand DeleteDaDataCommand { get; }
    public RelayCommand BackCommand { get; }

    private void SaveOpenAi()
    {
        _apiKeyStore.SaveApiKey(OpenAiApiKey);
        OpenAiApiKey = string.Empty;
        Message = "OpenAI API-ключ сохранён.";
        RefreshStatuses();
    }

    private void DeleteOpenAi()
    {
        _apiKeyStore.DeleteApiKey();
        Message = "OpenAI API-ключ удалён.";
        RefreshStatuses();
    }

    private void SaveDaData()
    {
        _daDataTokenStore.SaveToken(DaDataToken);
        DaDataToken = string.Empty;
        Message = "DaData API-ключ сохранён.";
        RefreshStatuses();
    }

    private void DeleteDaData()
    {
        _daDataTokenStore.DeleteToken();
        Message = "DaData API-ключ удалён.";
        RefreshStatuses();
    }

    private void RefreshStatuses()
    {
        OnPropertyChanged(nameof(OpenAiStatus));
        OnPropertyChanged(nameof(DaDataStatus));
        DeleteOpenAiCommand.RaiseCanExecuteChanged();
        DeleteDaDataCommand.RaiseCanExecuteChanged();
    }
}
