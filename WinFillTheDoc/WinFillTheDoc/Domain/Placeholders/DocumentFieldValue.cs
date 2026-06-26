using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinFillTheDoc.Domain.Placeholders;

public sealed class DocumentFieldValue : INotifyPropertyChanged
{
    private string _value = string.Empty;
    private FieldIssue? _issue;
    private FieldIssue? _referenceIssue;

    public DocumentFieldValue(PlaceholderDescriptor descriptor, ChoiceInputConfiguration? choiceConfiguration = null)
    {
        Descriptor = descriptor;
        ChoiceConfiguration = choiceConfiguration;
    }

    public PlaceholderDescriptor Descriptor { get; }
    public ChoiceInputConfiguration? ChoiceConfiguration { get; }
    public string Key => Descriptor.Key;
    public string Title => Descriptor.Title;
    public string Description => Descriptor.Description;
    public bool IsRequired => Descriptor.IsRequired;
    public bool IsChoice => Descriptor.InputKind == PlaceholderInputKind.Choice;
    public string SourceLabel => Descriptor.ValueSource?.GetLabel() ?? string.Empty;
    public IReadOnlyList<string> Options => ChoiceConfiguration?.Options ?? [];

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            Issue = null;
            OnPropertyChanged();
        }
    }

    public FieldIssue? Issue
    {
        get => _issue;
        private set
        {
            if (_issue == value) return;
            _issue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IssueText));
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(IsWarning));
        }
    }

    public string? IssueText => Issue?.Text ?? ReferenceIssue?.Text;
    public bool IsError => Issue?.Severity == FieldIssueSeverity.Error;
    public bool IsWarning => Issue?.Severity == FieldIssueSeverity.Warning || ReferenceIssue is not null;

    public FieldIssue? ReferenceIssue
    {
        get => _referenceIssue;
        private set
        {
            if (_referenceIssue == value) return;
            _referenceIssue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IssueText));
            OnPropertyChanged(nameof(IsWarning));
        }
    }

    public bool NormalizeAndValidate(PlaceholderFieldPolicy policy)
    {
        Value = policy.Normalize(Value);
        Issue = policy.Validate(Value);
        return !IsError;
    }

    public void ApplyReferenceIssue(string? message) =>
        ReferenceIssue = string.IsNullOrWhiteSpace(message) ? null : FieldIssue.Warning(message);

    public void ApplyReferenceValue(string value, PlaceholderFieldPolicy policy)
    {
        Value = policy.Normalize(value);
        ReferenceIssue = FieldIssue.Warning("Заменено на данные ФНС.");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
