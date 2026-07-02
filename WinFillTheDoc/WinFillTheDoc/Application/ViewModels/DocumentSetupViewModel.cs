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
    private readonly IPlaceholderCatalog _placeholderCatalog;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _templatePath;
    private string? _sourcePath;
    private string? _apiKeyStatusMessage;
    private string? _daDataTokenStatusMessage;
    private string? _templateStatusMessage;
    private string? _sourceStatusMessage;
    private bool _templateHasError;

    public DocumentSetupViewModel(
        IFileDialogService fileDialogService,
        IDocxTemplateService templateService,
        IApiKeyStore apiKeyStore,
        IDaDataTokenStore daDataTokenStore,
        IPlaceholderCatalog placeholderCatalog,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _fileDialogService = fileDialogService;
        _templateService = templateService;
        _placeholderCatalog = placeholderCatalog;
        _workflowState = workflowState;
        _navigationService = navigationService;
        _templatePath = workflowState.TemplateFile?.FullPath;
        _sourcePath = workflowState.SourceFile?.FullPath;

        SelectTemplateCommand = new RelayCommand(SelectTemplate);
        SelectSourceCommand = new RelayCommand(SelectSource);
        ContinueCommand = new RelayCommand(Continue, CanContinue);
        OpenPlaceholderLibraryCommand = new RelayCommand(() => _navigationService.NavigateTo<PlaceholderLibraryViewModel>());
        OpenSettingsCommand = new RelayCommand(() => _navigationService.NavigateTo<SettingsViewModel>());
        ApiKeyStatusMessage = apiKeyStore.HasApiKey
            ? "OpenAI ключ задан: автозаполнение доступно."
            : "OpenAI ключ не задан: автозаполнение будет пропущено.";
        DaDataTokenStatusMessage = daDataTokenStore.HasToken
            ? "DaData ключ задан: сверка ФНС доступна."
            : "DaData ключ не задан: сверка ФНС недоступна.";
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

    public string? ApiKeyStatusMessage
    {
        get => _apiKeyStatusMessage;
        private set => SetProperty(ref _apiKeyStatusMessage, value);
    }

    public string? DaDataTokenStatusMessage
    {
        get => _daDataTokenStatusMessage;
        private set => SetProperty(ref _daDataTokenStatusMessage, value);
    }

    public string? TemplateStatusMessage
    {
        get => _templateStatusMessage;
        private set => SetProperty(ref _templateStatusMessage, value);
    }

    public string? SourceStatusMessage
    {
        get => _sourceStatusMessage;
        private set => SetProperty(ref _sourceStatusMessage, value);
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
    public RelayCommand OpenPlaceholderLibraryCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }

    private void SelectTemplate()
    {
        var filePath = _fileDialogService.SelectTemplateFile();
        if (filePath is null) return;

        UseTemplateFile(filePath);
    }

    private void SelectSource()
    {
        var filePath = _fileDialogService.SelectSourceFile();
        if (filePath is null) return;

        UseSourceFile(filePath);
    }

    public void UseDroppedTemplate(string filePath)
    {
        if (!IsDocx(filePath))
        {
            TemplateHasError = true;
            TemplateStatusMessage = "Шаблон должен быть DOCX-файлом.";
            return;
        }

        UseTemplateFile(filePath);
    }

    public void UseDroppedSource(string filePath)
    {
        if (!IsSupportedSource(filePath))
        {
            SourceStatusMessage = "Файл реквизитов должен быть TXT, DOCX или PDF.";
            return;
        }

        UseSourceFile(filePath);
    }

    public static bool CanDropTemplate(string filePath) => IsDocx(filePath);
    public static bool CanDropSource(string filePath) => IsSupportedSource(filePath);

    private void UseTemplateFile(string filePath)
    {
        TemplatePath = filePath;
        _workflowState.TemplateFile = new DocumentFile(filePath);
        _workflowState.FieldValues = [];
        _workflowState.ExtractedValues = new Dictionary<string, string>();
        _workflowState.ExtractionStatusMessage = null;
        _workflowState.ResolvedValues = new Dictionary<string, string>();
        InspectTemplate(filePath);
    }

    private void UseSourceFile(string filePath)
    {
        SourcePath = filePath;
        SourceStatusMessage = "Файл реквизитов выбран.";
        _workflowState.SourceFile = new DocumentFile(filePath);
        _workflowState.FieldValues = [];
        _workflowState.ExtractedValues = new Dictionary<string, string>();
        _workflowState.ExtractionStatusMessage = null;
        _workflowState.ResolvedValues = new Dictionary<string, string>();
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
        _workflowState.TemplateFile = new DocumentFile(TemplatePath!);
        _workflowState.SourceFile = SourcePath is null ? null : new DocumentFile(SourcePath);
        _navigationService.NavigateTo<DocumentDataFormViewModel>();
    }

    private static bool IsDocx(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".docx", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedSource(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
