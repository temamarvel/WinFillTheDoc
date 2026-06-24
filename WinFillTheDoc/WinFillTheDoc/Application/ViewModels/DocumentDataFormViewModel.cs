using System.Collections.ObjectModel;
using System.ComponentModel;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class DocumentDataFormViewModel : ObservableObject
{
    private readonly IPlaceholderCatalog _placeholderCatalog;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _validationMessage;

    public DocumentDataFormViewModel(
        IPlaceholderCatalog placeholderCatalog,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _placeholderCatalog = placeholderCatalog;
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
    }

    public ObservableCollection<DocumentFieldValue> Fields { get; }
    public ObservableCollection<PlaceholderSectionGroup> Sections { get; }
    public string TemplateName => _workflowState.TemplateFile?.FileName ?? "Шаблон не выбран";

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand BackCommand { get; }
    public RelayCommand ConfirmCommand { get; }

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
}
