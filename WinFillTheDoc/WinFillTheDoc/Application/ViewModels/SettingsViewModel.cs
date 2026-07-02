using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IApiKeyStore _apiKeyStore;
    private readonly IDaDataTokenStore _daDataTokenStore;
    private readonly IAppUpdateChecker _appUpdateChecker;
    private readonly IAppVersionProvider _versionProvider;
    private readonly IExternalLinkService _externalLinkService;
    private readonly INavigationService _navigationService;
    private string _openAiApiKey = string.Empty;
    private string _daDataToken = string.Empty;
    private string? _message;
    private string? _updateStatus;
    private UpdateAvailability? _updateAvailability;

    public SettingsViewModel(
        IApiKeyStore apiKeyStore,
        IDaDataTokenStore daDataTokenStore,
        IAppUpdateChecker appUpdateChecker,
        IAppVersionProvider versionProvider,
        IExternalLinkService externalLinkService,
        INavigationService navigationService)
    {
        _apiKeyStore = apiKeyStore;
        _daDataTokenStore = daDataTokenStore;
        _appUpdateChecker = appUpdateChecker;
        _versionProvider = versionProvider;
        _externalLinkService = externalLinkService;
        _navigationService = navigationService;

        SaveOpenAiCommand = new RelayCommand(SaveOpenAi, () => !string.IsNullOrWhiteSpace(OpenAiApiKey));
        DeleteOpenAiCommand = new RelayCommand(DeleteOpenAi, () => _apiKeyStore.HasApiKey);
        SaveDaDataCommand = new RelayCommand(SaveDaData, () => !string.IsNullOrWhiteSpace(DaDataToken));
        DeleteDaDataCommand = new RelayCommand(DeleteDaData, () => _daDataTokenStore.HasToken);
        CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdatesAsync);
        OpenReleaseCommand = new RelayCommand(OpenRelease, () => UpdateAvailability is not null);
        DownloadUpdateCommand = new RelayCommand(DownloadUpdate, () => !string.IsNullOrWhiteSpace(UpdateAvailability?.DownloadUrl));
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
    public string CurrentVersionText => _versionProvider.CurrentVersion;

    public string? UpdateStatus
    {
        get => _updateStatus;
        private set => SetProperty(ref _updateStatus, value);
    }

    public UpdateAvailability? UpdateAvailability
    {
        get => _updateAvailability;
        private set
        {
            if (!SetProperty(ref _updateAvailability, value)) return;
            OnPropertyChanged(nameof(HasUpdate));
            OnPropertyChanged(nameof(UpdateTitle));
            OnPropertyChanged(nameof(UpdateNotes));
            OpenReleaseCommand.RaiseCanExecuteChanged();
            DownloadUpdateCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasUpdate => UpdateAvailability is not null;
    public string? UpdateTitle => UpdateAvailability is null ? null : $"Доступна версия {UpdateAvailability.LatestVersion}: {UpdateAvailability.ReleaseTitle}";
    public string? UpdateNotes => UpdateAvailability?.ReleaseNotes;

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public RelayCommand SaveOpenAiCommand { get; }
    public RelayCommand DeleteOpenAiCommand { get; }
    public RelayCommand SaveDaDataCommand { get; }
    public RelayCommand DeleteDaDataCommand { get; }
    public AsyncRelayCommand CheckUpdatesCommand { get; }
    public RelayCommand OpenReleaseCommand { get; }
    public RelayCommand DownloadUpdateCommand { get; }
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

    private async Task CheckUpdatesAsync()
    {
        UpdateStatus = "Проверка обновлений...";
        try
        {
            UpdateAvailability = await _appUpdateChecker.CheckForUpdateAsync();
            UpdateStatus = UpdateAvailability is null
                ? "Обновлений нет."
                : $"Доступна новая версия: {UpdateAvailability.LatestVersion}.";
        }
        catch (Exception exception)
        {
            UpdateAvailability = null;
            UpdateStatus = $"Ошибка проверки обновлений: {exception.Message}";
        }
    }

    private void OpenRelease()
    {
        if (UpdateAvailability?.ReleasePageUrl is { Length: > 0 } url)
            _externalLinkService.Open(url);
    }

    private void DownloadUpdate()
    {
        if (UpdateAvailability?.DownloadUrl is { Length: > 0 } url)
            _externalLinkService.Open(url);
    }

    private void RefreshStatuses()
    {
        OnPropertyChanged(nameof(OpenAiStatus));
        OnPropertyChanged(nameof(DaDataStatus));
        DeleteOpenAiCommand.RaiseCanExecuteChanged();
        DeleteDaDataCommand.RaiseCanExecuteChanged();
    }
}
