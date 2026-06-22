using System.Collections.ObjectModel;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class ConfirmationViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public ConfirmationViewModel(DocumentWorkflowState workflowState, INavigationService navigationService)
    {
        _navigationService = navigationService;
        TemplateName = workflowState.TemplateFile?.FileName ?? "Шаблон не выбран";
        FieldValues = new ObservableCollection<DocumentFieldValue>(workflowState.FieldValues);
        StartOverCommand = new RelayCommand(() => _navigationService.NavigateTo<DocumentSetupViewModel>());
        EditCommand = new RelayCommand(() => _navigationService.NavigateTo<DocumentDataFormViewModel>());
    }

    public string TemplateName { get; }
    public ObservableCollection<DocumentFieldValue> FieldValues { get; }
    public RelayCommand StartOverCommand { get; }
    public RelayCommand EditCommand { get; }
}
