using System.Collections.ObjectModel;
using System.ComponentModel;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class DocumentDataFormViewModel : ObservableObject
{
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private string? _validationMessage;

    public DocumentDataFormViewModel(
        IPlaceholderCatalog placeholderCatalog,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _workflowState = workflowState;
        _navigationService = navigationService;
        Fields = new ObservableCollection<DocumentFieldValue>(placeholderCatalog.GetAll().Select(x => new DocumentFieldValue(x)));
        foreach (var field in Fields)
        {
            field.PropertyChanged += OnFieldPropertyChanged;
        }

        BackCommand = new RelayCommand(() => _navigationService.NavigateTo<DocumentSetupViewModel>());
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public ObservableCollection<DocumentFieldValue> Fields { get; }
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
        var isValid = Fields.All(x => x.Validate());
        if (!isValid)
        {
            ValidationMessage = "Заполните обязательные поля, отмеченные звездочкой.";
            return;
        }

        ValidationMessage = null;
        _workflowState.FieldValues = Fields.ToList();
        _navigationService.NavigateTo<ConfirmationViewModel>();
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentFieldValue.Value) && ValidationMessage is not null)
        {
            ValidationMessage = null;
        }
    }
}
