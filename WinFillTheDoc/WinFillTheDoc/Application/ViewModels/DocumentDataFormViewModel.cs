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
    private readonly IDaDataTokenStore _daDataTokenStore;
    private readonly ICompanyReferenceValidator _companyReferenceValidator;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _validationMessage;
    private string? _extractionMessage;
    private ExtractionStatusKind _extractionStatusKind;
    private string? _referenceValidationMessage;
    private bool _isExtracting;
    private bool _isCheckingReference;
    private CompanyReferenceResolution _lastReferenceResolution = CompanyReferenceResolution.Empty;

    public DocumentDataFormViewModel(
        IPlaceholderCatalog placeholderCatalog,
        IDocumentTextExtractor textExtractor,
        IRequisitesExtractionService requisitesExtractionService,
        IApiKeyStore apiKeyStore,
        IDaDataTokenStore daDataTokenStore,
        ICompanyReferenceValidator companyReferenceValidator,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _placeholderCatalog = placeholderCatalog;
        _textExtractor = textExtractor;
        _requisitesExtractionService = requisitesExtractionService;
        _apiKeyStore = apiKeyStore;
        _daDataTokenStore = daDataTokenStore;
        _companyReferenceValidator = companyReferenceValidator;
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
        CheckReferenceCommand = new AsyncRelayCommand(CheckReferenceAsync, CanCheckReference);
        ApplyReferenceValuesCommand = new RelayCommand(ApplyReferenceValues, CanApplyReferenceValues);
        ExtractionMessage = workflowState.ExtractionStatusMessage;
        ReferenceValidationMessage = daDataTokenStore.HasToken
            ? "Сверку с ФНС можно выполнить после заполнения ИНН или ОГРН."
            : "DaData API-ключ не задан. Сверка с ФНС недоступна.";

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

    public ExtractionStatusKind ExtractionStatusKind
    {
        get => _extractionStatusKind;
        private set => SetProperty(ref _extractionStatusKind, value);
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

    public string? ReferenceValidationMessage
    {
        get => _referenceValidationMessage;
        private set => SetProperty(ref _referenceValidationMessage, value);
    }

    public bool IsCheckingReference
    {
        get => _isCheckingReference;
        private set
        {
            if (!SetProperty(ref _isCheckingReference, value)) return;
            CheckReferenceCommand.RaiseCanExecuteChanged();
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
    public AsyncRelayCommand CheckReferenceCommand { get; }
    public RelayCommand ApplyReferenceValuesCommand { get; }

    private async Task LoadExtractedValuesAsync()
    {
        if (_workflowState.SourceFile is null) return;

        IsExtracting = true;
        try
        {
            var extraction = await _textExtractor.ExtractAsync(_workflowState.SourceFile.FullPath);
            if (string.IsNullOrWhiteSpace(extraction.Text))
            {
                var reason = extraction.NeedsOcr
                    ? "PDF похож на скан, OCR пока не поддержан."
                    : extraction.Diagnostics.Errors.FirstOrDefault()
                      ?? extraction.Diagnostics.Notes.FirstOrDefault()
                      ?? "Текст не найден.";
                SetExtractionMessage($"Автозаполнение недоступно: {reason} Заполните форму вручную.", extraction.NeedsOcr ? ExtractionStatusKind.Warning : ExtractionStatusKind.Error);
                return;
            }

            if (!_apiKeyStore.HasApiKey)
            {
                SetExtractionMessage($"Текст извлечён: {extraction.Diagnostics.ProducedChars} символов. OpenAI API-ключ не задан, заполните форму вручную.", ExtractionStatusKind.Warning);
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
                : $"Автозаполнение выполнено. Текст извлечён: {extraction.Diagnostics.ProducedChars} символов. Найдено полей: {extractedValues.Count}. Проверьте значения перед подтверждением.",
                extractedValues.Count == 0 ? ExtractionStatusKind.Warning : ExtractionStatusKind.Success);
        }
        catch (Exception exception)
        {
            SetExtractionMessage($"Не удалось выполнить автозаполнение: {exception.Message}. Заполните форму вручную.", ExtractionStatusKind.Error);
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

    private async Task CheckReferenceAsync()
    {
        if (!_daDataTokenStore.HasToken)
        {
            ReferenceValidationMessage = "DaData API-ключ не задан. Вернитесь назад и укажите ключ.";
            return;
        }

        IsCheckingReference = true;
        try
        {
            ClearReferenceIssues();
            var resolution = await _companyReferenceValidator.ResolveAsync(CurrentValues());
            _lastReferenceResolution = resolution;
            ApplyReferenceIssues(resolution.Issues);

            ReferenceValidationMessage = resolution.ReferenceValues.Count == 0
                ? "Компания не найдена в DaData/ФНС или сервис временно недоступен."
                : resolution.Issues.Count == 0
                    ? "Сверка выполнена. Расхождений с ФНС не найдено."
                    : $"Сверка выполнена. Найдено расхождений: {resolution.Issues.Count}.";
            ApplyReferenceValuesCommand.RaiseCanExecuteChanged();
        }
        catch (Exception exception)
        {
            ReferenceValidationMessage = $"Не удалось выполнить сверку с ФНС: {exception.Message}";
        }
        finally
        {
            IsCheckingReference = false;
        }
    }

    private void ApplyReferenceValues()
    {
        var companyKeys = Fields
            .Where(x => x.Descriptor.Section == PlaceholderSection.Company)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var field in Fields.Where(x => companyKeys.Contains(x.Key)))
        {
            if (!_lastReferenceResolution.ReferenceValues.TryGetValue(field.Key, out var value)) continue;

            field.ApplyReferenceValue(value, _placeholderCatalog.GetFieldPolicy(field.Key));
        }

        ReferenceValidationMessage = "Официальные данные ФНС применены к полям компании. Проверьте значения перед подтверждением.";
        ApplyReferenceValuesCommand.RaiseCanExecuteChanged();
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

        if (e.PropertyName == nameof(DocumentFieldValue.Value))
        {
            _lastReferenceResolution = CompanyReferenceResolution.Empty;
            ApplyReferenceValuesCommand.RaiseCanExecuteChanged();
            CheckReferenceCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanExtractFromSource() => _workflowState.SourceFile is not null && !IsExtracting;
    private bool CanCheckReference() => _daDataTokenStore.HasToken && !IsCheckingReference && HasReferenceLookupValue();
    private bool CanApplyReferenceValues() => _lastReferenceResolution.ReferenceValues.Count > 0;

    private void ApplyExtractedValues(IReadOnlyDictionary<string, string> extractedValues)
    {
        foreach (var field in Fields)
        {
            if (!extractedValues.TryGetValue(field.Key, out var value)) continue;

            var policy = _placeholderCatalog.GetFieldPolicy(field.Key);
            field.Value = policy.Normalize(value);
        }
    }

    private IReadOnlyDictionary<string, string> CurrentValues() =>
        Fields.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

    private bool HasReferenceLookupValue() =>
        Fields.Any(x => (x.Key == "inn" || x.Key == "ogrn") && !string.IsNullOrWhiteSpace(x.Value));

    private void ApplyReferenceIssues(IReadOnlyDictionary<string, FieldReferenceIssue> issues)
    {
        foreach (var field in Fields)
            field.ApplyReferenceIssue(issues.TryGetValue(field.Key, out var issue) ? issue.Message : null);
    }

    private void ClearReferenceIssues()
    {
        foreach (var field in Fields) field.ApplyReferenceIssue(null);
    }

    private void SetExtractionMessage(string message, ExtractionStatusKind kind)
    {
        ExtractionMessage = message;
        ExtractionStatusKind = kind;
        _workflowState.ExtractionStatusMessage = message;
    }
}
