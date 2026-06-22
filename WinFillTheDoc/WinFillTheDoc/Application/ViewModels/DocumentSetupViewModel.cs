using System.IO;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Documents;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class DocumentSetupViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _templatePath;
    private string? _sourcePath;

    public DocumentSetupViewModel(
        IFileDialogService fileDialogService,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _fileDialogService = fileDialogService;
        _workflowState = workflowState;
        _navigationService = navigationService;

        SelectTemplateCommand = new RelayCommand(SelectTemplate);
        SelectSourceCommand = new RelayCommand(SelectSource);
        ContinueCommand = new RelayCommand(Continue, () => !string.IsNullOrWhiteSpace(TemplatePath));
    }

    public string? TemplatePath
    {
        get => _templatePath;
        private set
        {
            if (SetProperty(ref _templatePath, value))
            {
                OnPropertyChanged(nameof(TemplateFileName));
                ContinueCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? SourcePath
    {
        get => _sourcePath;
        private set
        {
            if (SetProperty(ref _sourcePath, value))
            {
                OnPropertyChanged(nameof(SourceFileName));
            }
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
        if (filePath is not null)
        {
            TemplatePath = filePath;
        }
    }

    private void SelectSource()
    {
        var filePath = _fileDialogService.SelectSourceFile();
        if (filePath is not null)
        {
            SourcePath = filePath;
        }
    }

    private void Continue()
    {
        _workflowState.TemplateFile = new DocumentFile(TemplatePath!);
        _workflowState.SourceFile = SourcePath is null ? null : new DocumentFile(SourcePath);
        _navigationService.NavigateTo<DocumentDataFormViewModel>();
    }
}
