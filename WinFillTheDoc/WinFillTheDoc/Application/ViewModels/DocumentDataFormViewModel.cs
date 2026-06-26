using System.Collections.ObjectModel;
using System.ComponentModel;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class DocumentDataFormViewModel : ObservableObject
{
    private readonly IPlaceholderCatalog _placeholderCatalog;
    private readonly IDocumentTextExtractor _textExtractor;
    private readonly IRequisitesExtractionService _requisitesExtractionService;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _validationMessage;
    private string? _extractionMessage;
    private bool _isExtracting;

    public DocumentDataFormViewModel(
        IPlaceholderCatalog placeholderCatalog,
        IDocumentTextExtractor textExtractor,
        IRequisitesExtractionService requisitesExtractionService,
        IApiKeyStore apiKeyStore,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _placeholderCatalog = placeholderCatalog;
        _textExtractor = textExtractor;
        _requisitesExtractionService = requisitesExtractionService;
        _apiKeyStore = apiKeyStore;
        _workflowState = workflowState;
        _navigationService = navigationService;
        Fields = workflowState.FieldValues.Count > 0
            ? new ObservableCollection<DocumentFieldValue>(workflowState.FieldValues)
            : new ObservableCollection<DocumentFieldValue>(placeholderCatalog.GetInputDescriptors()
                .Select(x => new DocumentFieldValue(x, placeholderCatalog.GetChoiceConfiguration(x.Key))));
        Sections = new ObservableCollection<PlaceholderSectionGroup>(Fields
            .GroupBy(x => x.Descriptor.Section)
            .OrderBy(x => x.Key)
            .Select(x => new PlaceholderSectionGroup(x.Key, x)));

        foreach (var field in Fields) field.PropertyChanged += OnFieldPropertyChanged;

        BackCommand = new RelayCommand(GoBack);
        ConfirmCommand = new RelayCommand(Confirm);
        RefillFromSourceCommand = new AsyncRelayCommand(LoadExtractedValuesAsync, CanExtractFromSource);
        ExtractionMessage = workflowState.ExtractionStatusMessage;

        if (workflowState.FieldValues.Count == 0 && workflowState.ExtractedValues.Count > 0)
            ApplyExtractedValues(workflowState.ExtractedValues);

        if (workflowState.FieldValues.Count == 0 && workflowState.ExtractedValues.Count == 0 && CanExtractFromSource())
            _ = LoadExtractedValuesAsync();
    }

    public ObservableCollection<DocumentFieldValue> Fields { get; }
    public ObservableCollection<PlaceholderSectionGroup> Sections { get; }
    public string TemplateName => _workflowState.TemplateFile?.FileName ?? "Шаблон не выбран";
    public string SourceName => _workflowState.SourceFile?.FileName ?? "Файл реквизитов не выбран";

    public string? ExtractionMessage
    {
        get => _extractionMessage;
        private set => SetProperty(ref _extractionMessage, value);
    }

    public bool IsExtracting
    {
        get => _isExtracting;
        private set
        {
            if (!SetProperty(ref _isExtracting, value)) return;
            RefillFromSourceCommand.RaiseCanExecuteChanged();
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand BackCommand { get; }
    public RelayCommand ConfirmCommand { get; }
    public AsyncRelayCommand RefillFromSourceCommand { get; }

    private async Task LoadExtractedValuesAsync()
    {
        if (_workflowState.SourceFile is null) return;

        IsExtracting = true;
        try
        {
            var extraction = await _textExtractor.ExtractAsync(_workflowState.SourceFile.FullPath);
            if (string.IsNullOrWhiteSpace(extraction.Text))
            {
                var reason = extraction.Diagnostics.Errors.FirstOrDefault()
                    ?? extraction.Diagnostics.Notes.FirstOrDefault()
                    ?? "Текст не найден.";
                SetExtractionMessage($"Автозаполнение недоступно: {reason} Заполните форму вручную.");
                return;
            }

            if (!_apiKeyStore.HasApiKey)
            {
                SetExtractionMessage("Текст из файла извлечён, но OpenAI API-ключ не задан. Заполните форму вручную или вернитесь назад и укажите ключ.");
                return;
            }

            var descriptors = _placeholderCatalog.GetAll()
                .Where(x => x.ValueSource == PlaceholderValueSource.Extracted)
                .OrderBy(x => x.Section)
                .ThenBy(x => x.Order)
                .ToList();
            var extractedValues = await _requisitesExtractionService.ExtractAsync(extraction.Text, descriptors);
            _workflowState.ExtractedValues = extractedValues;
            ApplyExtractedValues(extractedValues);
            SetExtractionMessage(extractedValues.Count == 0
                ? "OpenAI не нашёл реквизиты в документе. Заполните форму вручную."
                : $"Автозаполнение выполнено. Найдено полей: {extractedValues.Count}. Проверьте значения перед подтверждением.");
        }
        catch (Exception exception)
        {
            SetExtractionMessage($"Не удалось выполнить автозаполнение: {exception.Message}. Заполните форму вручную.");
        }
        finally
        {
            IsExtracting = false;
        }
    }

    private void Confirm()
    {
        var isValid = Fields.All(x => x.NormalizeAndValidate(_placeholderCatalog.GetFieldPolicy(x.Key)));
        if (!isValid)
        {
            ValidationMessage = "Исправьте поля, отмеченные ошибкой.";
            return;
        }

        ValidationMessage = null;
        _workflowState.FieldValues = Fields.ToList();
        _navigationService.NavigateTo<ConfirmationViewModel>();
    }

    private void GoBack()
    {
        _workflowState.FieldValues = Fields.ToList();
        _navigationService.NavigateTo<DocumentSetupViewModel>();
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentFieldValue.Value) && ValidationMessage is not null)
            ValidationMessage = null;
    }

    private bool CanExtractFromSource() => _workflowState.SourceFile is not null && !IsExtracting;

    private void ApplyExtractedValues(IReadOnlyDictionary<string, string> extractedValues)
    {
        foreach (var field in Fields)
        {
            if (!extractedValues.TryGetValue(field.Key, out var value)) continue;

            var policy = _placeholderCatalog.GetFieldPolicy(field.Key);
            field.Value = policy.Normalize(value);
        }
    }

    private void SetExtractionMessage(string message)
    {
        ExtractionMessage = message;
        _workflowState.ExtractionStatusMessage = message;
    }
}
