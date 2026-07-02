using System.Collections.ObjectModel;
using System.IO;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class ConfirmationViewModel : ObservableObject
{
    private readonly DocumentWorkflowState _workflowState;
    private readonly IFileDialogService _fileDialogService;
    private readonly IDocxTemplateService _templateService;
    private readonly IClipboardService _clipboardService;
    private readonly IExternalLinkService _externalLinkService;
    private readonly INavigationService _navigationService;
    private string? _generationStatus;
    private string? _copyStatus;
    private string? _lastGeneratedDocumentPath;
    private bool _isGenerating;

    public ConfirmationViewModel(
        DocumentWorkflowState workflowState,
        PlaceholderValueAssembler valueAssembler,
        IPlaceholderCatalog placeholderCatalog,
        IFileDialogService fileDialogService,
        IDocxTemplateService templateService,
        IDocumentDataCopyStringBuilder copyStringBuilder,
        IClipboardService clipboardService,
        IExternalLinkService externalLinkService,
        INavigationService navigationService)
    {
        _workflowState = workflowState;
        _fileDialogService = fileDialogService;
        _templateService = templateService;
        _clipboardService = clipboardService;
        _externalLinkService = externalLinkService;
        _navigationService = navigationService;
        TemplateName = workflowState.TemplateFile?.FileName ?? "Шаблон не выбран";
        workflowState.ResolvedValues = valueAssembler.Assemble(workflowState.FieldValues);
        CopyString = copyStringBuilder.BuildRow(workflowState.ResolvedValues);
        CopyStringPreview = CopyString.Replace("\t", " | ");
        FieldValues = new ObservableCollection<ResolvedPlaceholderValue>(workflowState.ResolvedValues
            .OrderBy(x => placeholderCatalog.GetAll().FirstOrDefault(d => d.Key == x.Key)?.Order ?? int.MaxValue)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new ResolvedPlaceholderValue(
                placeholderCatalog.GetAll().FirstOrDefault(d => d.Key == x.Key)?.Title ?? x.Key,
                x.Value)));

        StartOverCommand = new RelayCommand(StartOver);
        EditCommand = new RelayCommand(() => _navigationService.NavigateTo<DocumentDataFormViewModel>());
        CopyStringCommand = new RelayCommand(CopyStringToClipboard, () => !string.IsNullOrWhiteSpace(CopyString));
        GenerateCommand = new AsyncRelayCommand(GenerateDocumentAsync, () => !IsGenerating && workflowState.TemplateInspection?.CanGenerate == true);
        OpenGeneratedDocumentCommand = new RelayCommand(OpenGeneratedDocument, () => LastGeneratedDocumentPath is not null);
        OpenGeneratedFolderCommand = new RelayCommand(OpenGeneratedFolder, () => LastGeneratedDocumentPath is not null);
    }

    public string TemplateName { get; }
    public ObservableCollection<ResolvedPlaceholderValue> FieldValues { get; }
    public string CopyString { get; }
    public string CopyStringPreview { get; }
    public string? GenerationStatus
    {
        get => _generationStatus;
        private set => SetProperty(ref _generationStatus, value);
    }

    public string? CopyStatus
    {
        get => _copyStatus;
        private set => SetProperty(ref _copyStatus, value);
    }

    public string? LastGeneratedDocumentPath
    {
        get => _lastGeneratedDocumentPath;
        private set
        {
            if (!SetProperty(ref _lastGeneratedDocumentPath, value)) return;
            OpenGeneratedDocumentCommand.RaiseCanExecuteChanged();
            OpenGeneratedFolderCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (!SetProperty(ref _isGenerating, value)) return;
            GenerateCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand StartOverCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand CopyStringCommand { get; }
    public AsyncRelayCommand GenerateCommand { get; }
    public RelayCommand OpenGeneratedDocumentCommand { get; }
    public RelayCommand OpenGeneratedFolderCommand { get; }

    private async Task GenerateDocumentAsync()
    {
        var templatePath = _workflowState.TemplateFile?.FullPath;
        if (templatePath is null)
        {
            GenerationStatus = "Шаблон не выбран.";
            return;
        }

        var defaultName = $"{Path.GetFileNameWithoutExtension(templatePath)}_filled.docx";
        var outputPath = _fileDialogService.SelectOutputFile(defaultName);
        if (outputPath is null) return;

        IsGenerating = true;
        GenerationStatus = "Создание документа...";
        try
        {
            var result = await Task.Run(() => _templateService.Generate(templatePath, outputPath, _workflowState.ResolvedValues));
            LastGeneratedDocumentPath = result.OutputPath;
            CopyStringToClipboard("Строка для Google Sheets скопирована после генерации.");
            GenerationStatus = $"Документ сохранен: {result.OutputPath}. Замен выполнено: {result.ReplacementsCount}.";
        }
        catch (Exception exception)
        {
            GenerationStatus = $"Не удалось создать документ: {exception.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private void CopyStringToClipboard() => CopyStringToClipboard("Строка для Google Sheets скопирована.");

    private void CopyStringToClipboard(string status)
    {
        if (string.IsNullOrWhiteSpace(CopyString)) return;

        _clipboardService.SetText(CopyString);
        CopyStatus = status;
    }

    private void OpenGeneratedDocument()
    {
        if (LastGeneratedDocumentPath is not null)
            _externalLinkService.OpenFile(LastGeneratedDocumentPath);
    }

    private void OpenGeneratedFolder()
    {
        if (LastGeneratedDocumentPath is null) return;

        var folder = Path.GetDirectoryName(LastGeneratedDocumentPath);
        if (folder is not null) _externalLinkService.OpenFolder(folder);
    }

    private void StartOver()
    {
        _workflowState.FieldValues = [];
        _workflowState.ResolvedValues = new Dictionary<string, string>();
        _navigationService.NavigateTo<DocumentSetupViewModel>();
    }
}
