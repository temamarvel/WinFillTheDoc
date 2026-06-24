namespace WinFillTheDoc.Domain.Placeholders;

public interface IPlaceholderCatalog
{
    IReadOnlyList<PlaceholderDescriptor> GetAll();
    IReadOnlyList<PlaceholderDescriptor> GetInputDescriptors();
    PlaceholderFieldPolicy GetFieldPolicy(string key);
    ChoiceInputConfiguration? GetChoiceConfiguration(string key);
}
