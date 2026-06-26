using System.Collections.ObjectModel;
using WinFillTheDoc.Application.Navigation;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class PlaceholderLibraryViewModel : ObservableObject
{
    private readonly IPlaceholderCatalog _placeholderCatalog;
    private readonly ICustomPlaceholderRepository _customPlaceholderRepository;
    private readonly CustomPlaceholderValidator _validator;
    private readonly DocumentWorkflowState _workflowState;
    private readonly INavigationService _navigationService;
    private PlaceholderLibraryItem? _selectedItem;
    private string _key = string.Empty;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private PlaceholderSection _section = PlaceholderSection.Custom;
    private PlaceholderValueSource _valueSource = PlaceholderValueSource.Manual;
    private PlaceholderInputKind _inputKind = PlaceholderInputKind.Text;
    private bool _isRequired;
    private string _optionsText = string.Empty;
    private string? _message;
    private string? _editingOriginalKey;

    public PlaceholderLibraryViewModel(
        IPlaceholderCatalog placeholderCatalog,
        ICustomPlaceholderRepository customPlaceholderRepository,
        CustomPlaceholderValidator validator,
        DocumentWorkflowState workflowState,
        INavigationService navigationService)
    {
        _placeholderCatalog = placeholderCatalog;
        _customPlaceholderRepository = customPlaceholderRepository;
        _validator = validator;
        _workflowState = workflowState;
        _navigationService = navigationService;

        Items = [];
        Sections = Enum.GetValues<PlaceholderSection>();
        Sources = Enum.GetValues<PlaceholderValueSource>();
        InputKinds = Enum.GetValues<PlaceholderInputKind>();

        NewCommand = new RelayCommand(New);
        SaveCommand = new RelayCommand(Save, CanSave);
        DeleteCommand = new RelayCommand(Delete, CanDelete);
        BackCommand = new RelayCommand(() => _navigationService.NavigateTo<DocumentSetupViewModel>());

        Reload();
        New();
    }

    public ObservableCollection<PlaceholderLibraryItem> Items { get; }
    public IReadOnlyList<PlaceholderSection> Sections { get; }
    public IReadOnlyList<PlaceholderValueSource> Sources { get; }
    public IReadOnlyList<PlaceholderInputKind> InputKinds { get; }

    public PlaceholderLibraryItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value)) return;
            LoadSelected(value);
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public string Key
    {
        get => _key;
        set
        {
            if (!SetProperty(ref _key, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (!SetProperty(ref _title, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public PlaceholderSection Section
    {
        get => _section;
        set => SetProperty(ref _section, value);
    }

    public PlaceholderValueSource ValueSource
    {
        get => _valueSource;
        set => SetProperty(ref _valueSource, value);
    }

    public PlaceholderInputKind InputKind
    {
        get => _inputKind;
        set
        {
            if (!SetProperty(ref _inputKind, value)) return;
            OnPropertyChanged(nameof(IsChoice));
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsRequired
    {
        get => _isRequired;
        set => SetProperty(ref _isRequired, value);
    }

    public string OptionsText
    {
        get => _optionsText;
        set
        {
            if (!SetProperty(ref _optionsText, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public bool IsEditingBuiltIn => SelectedItem is { IsCustom: false };
    public bool IsChoice => InputKind == PlaceholderInputKind.Choice;

    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand BackCommand { get; }

    private void Reload()
    {
        Items.Clear();
        var customKeys = _customPlaceholderRepository.GetAll().Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var descriptor in _placeholderCatalog.GetAll())
        {
            var configuration = _placeholderCatalog.GetChoiceConfiguration(descriptor.Key);
            Items.Add(new PlaceholderLibraryItem(
                descriptor.Key,
                descriptor.Title,
                descriptor.Description,
                descriptor.Section,
                descriptor.ValueSource,
                descriptor.InputKind,
                descriptor.IsRequired,
                configuration?.Options ?? [],
                customKeys.Contains(descriptor.Key)));
        }
    }

    private void New()
    {
        SelectedItem = null;
        _editingOriginalKey = null;
        Key = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        Section = PlaceholderSection.Custom;
        ValueSource = PlaceholderValueSource.Manual;
        InputKind = PlaceholderInputKind.Text;
        IsRequired = false;
        OptionsText = string.Empty;
        Message = null;
        OnPropertyChanged(nameof(IsEditingBuiltIn));
        SaveCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private void LoadSelected(PlaceholderLibraryItem? item)
    {
        if (item is null) return;

        _editingOriginalKey = item.IsCustom ? item.Key : null;
        Key = item.Key;
        Title = item.Title;
        Description = item.Description;
        Section = item.Section;
        ValueSource = item.ValueSource ?? PlaceholderValueSource.Manual;
        InputKind = item.InputKind;
        IsRequired = item.IsRequired;
        OptionsText = string.Join(Environment.NewLine, item.Options);
        Message = item.IsCustom ? null : "Встроенные плейсхолдеры доступны только для просмотра.";
        OnPropertyChanged(nameof(IsEditingBuiltIn));
        SaveCommand.RaiseCanExecuteChanged();
    }

    private bool CanSave() => !IsEditingBuiltIn;
    private bool CanDelete() => SelectedItem is { IsCustom: true };

    private void Save()
    {
        var definition = BuildDefinition();
        var existingKeys = _placeholderCatalog.GetAll().Select(x => x.Key);
        var result = _validator.Validate(definition, existingKeys, _editingOriginalKey);
        if (!result.IsValid)
        {
            Message = string.Join(Environment.NewLine, result.Errors);
            return;
        }

        if (_editingOriginalKey is null)
            _customPlaceholderRepository.Add(definition);
        else
            _customPlaceholderRepository.Update(_editingOriginalKey, definition);

        InvalidateWorkflow();
        Reload();
        SelectedItem = Items.FirstOrDefault(x => x.Key == definition.Key);
        Message = "Пользовательский плейсхолдер сохранён.";
    }

    private void Delete()
    {
        if (SelectedItem is not { IsCustom: true } item) return;

        _customPlaceholderRepository.Delete(item.Key);
        InvalidateWorkflow();
        Reload();
        New();
        Message = "Пользовательский плейсхолдер удалён.";
    }

    private CustomPlaceholderDefinition BuildDefinition() => new(
        Key.Trim(),
        Title.Trim(),
        Description.Trim(),
        Section,
        ValueSource,
        InputKind,
        IsRequired,
        OptionsText
            .Split([Environment.NewLine, "\n", ","], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList());

    private void InvalidateWorkflow()
    {
        _workflowState.FieldValues = [];
        _workflowState.TemplateInspection = null;
        _workflowState.ExtractedValues = new Dictionary<string, string>();
        _workflowState.ResolvedValues = new Dictionary<string, string>();
    }
}
