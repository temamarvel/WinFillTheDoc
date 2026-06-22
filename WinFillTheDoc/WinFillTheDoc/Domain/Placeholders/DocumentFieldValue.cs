using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinFillTheDoc.Domain.Placeholders;

public sealed class DocumentFieldValue : INotifyPropertyChanged
{
    private string _value = string.Empty;
    private string? _error;

    public DocumentFieldValue(PlaceholderDefinition definition)
    {
        Definition = definition;
    }

    public PlaceholderDefinition Definition { get; }
    public string Key => Definition.Key;
    public string Title => Definition.Title;
    public bool IsRequired => Definition.IsRequired;

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (_error == value) return;
            _error = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public bool Validate()
    {
        Error = IsRequired && string.IsNullOrWhiteSpace(Value) ? "Поле обязательно для заполнения." : null;
        return !HasError;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
