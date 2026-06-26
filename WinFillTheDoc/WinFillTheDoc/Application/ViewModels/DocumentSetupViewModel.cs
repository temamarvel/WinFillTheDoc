using System.IO;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Documents;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class DocumentSetupViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IDocxTemplateService _templateService;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly IPlaceholderCatalog _placeholderCatalog;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _templatePath;
    private string? _sourcePath;
    private string _apiKey = string.Empty;
    private string? _apiKeyStatusMessage;
    private string? _templateStatusMessage;
    private bool _templateHasError;

    public DocumentSetupViewModel(
        IFileDialogService fileDialogService,
        IDocxTemplateService templateService,
        IApiKeyStore apiKeyStore,
        IPlaceholderCatalog placeholderCatalog,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _fileDialogService = fileDialogService;
        _templateService = templateService;
        _apiKeyStore = apiKeyStore;
        _placeholderCatalog = placeholderCatalog;
        _workflowState = workflowState;
        _navigationService = navigationService;
        _templatePath = workflowState.TemplateFile?.FullPath;
        _sourcePath = workflowState.SourceFile?.FullPath;

        SelectTemplateCommand = new RelayCommand(SelectTemplate);
        SelectSourceCommand = new RelayCommand(SelectSource);
        ContinueCommand = new RelayCommand(Continue, CanContinue);
        ApiKeyStatusMessage = apiKeyStore.HasApiKey
            ? "OpenAI API-ключ сохранён. Оставьте поле пустым, чтобы не менять его."
            : "API-ключ не задан. Автозаполнение будет пропущено, форму можно заполнить вручную.";
    }

    public string? TemplatePath
    {
        get => _templatePath;
        private set
        {
            if (!SetProperty(ref _templatePath, value)) return;
            OnPropertyChanged(nameof(TemplateFileName));
            ContinueCommand.RaiseCanExecuteChanged();
        }
    }

    public string? SourcePath
    {
        get => _sourcePath;
        private set
        {
            if (!SetProperty(ref _sourcePath, value)) return;
            OnPropertyChanged(nameof(SourceFileName));
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string? ApiKeyStatusMessage
    {
        get => _apiKeyStatusMessage;
        private set => SetProperty(ref _apiKeyStatusMessage, value);
    }

    public string? TemplateStatusMessage
    {
        get => _templateStatusMessage;
        private set => SetProperty(ref _templateStatusMessage, value);
    }

    public bool TemplateHasError
    {
        get => _templateHasError;
        private set
        {
            if (SetProperty(ref _templateHasError, value)) ContinueCommand.RaiseCanExecuteChanged();
        }
    }

    public string TemplateFileName => TemplatePath is null ? "Файл не выбран" : Path.GetFileName(TemplatePath);
    public string SourceFileName => SourcePath is null ? "Файл не выбран (можно добавить позже)" : Path.GetFileName(SourcePath);
    public RelayCommand SelectTemplateCommand { get; }
    public RelayCommand SelectSourceCommand { get; }
    public RelayCommand ContinueCommand { get; }

    private void SelectTemplate()
    {
        var filePath = _fileDialogService.SelectTemplateFile();
        if (filePath is null) return;

        TemplatePath = filePath;
        _workflowState.FieldValues = [];
        InspectTemplate(filePath);
    }

    private void SelectSource()
    {
        var filePath = _fileDialogService.SelectSourceFile();
        if (filePath is null) return;

        SourcePath = filePath;
        _workflowState.ExtractedValues = new Dictionary<string, string>();
        _workflowState.ExtractionStatusMessage = null;
    }

    private void InspectTemplate(string filePath)
    {
        try
        {
            var knownKeys = _placeholderCatalog.GetAll().Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
            var inspection = _templateService.Inspect(filePath, knownKeys);
            _workflowState.TemplateInspection = inspection;
            TemplateHasError = !inspection.CanGenerate;
            TemplateStatusMessage = inspection switch
            {
                { ProcessingIssues.Count: > 0 } => "Не удалось обработать DOCX-шаблон.",
                { UnknownKeys.Count: > 0 } => $"Неизвестные плейсхолдеры: {string.Join(", ", inspection.UnknownKeys)}.",
                { HasPlaceholders: false } => "В шаблоне не найдены плейсхолдеры <!ключ!>.",
                _ => $"Найдено плейсхолдеров: {inspection.FoundKeys.Count}.",
            };
        }
        catch (Exception exception)
        {
            _workflowState.TemplateInspection = null;
            TemplateHasError = true;
            TemplateStatusMessage = $"Не удалось прочитать шаблон: {exception.Message}";
        }
    }

    private bool CanContinue() => !string.IsNullOrWhiteSpace(TemplatePath) && !TemplateHasError;

    private void Continue()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            _apiKeyStore.SaveApiKey(ApiKey);
            ApiKey = string.Empty;
            ApiKeyStatusMessage = "OpenAI API-ключ сохранён.";
        }

        _workflowState.TemplateFile = new DocumentFile(TemplatePath!);
        _workflowState.SourceFile = SourcePath is null ? null : new DocumentFile(SourcePath);
        _navigationService.NavigateTo<DocumentDataFormViewModel>();
    }
}
