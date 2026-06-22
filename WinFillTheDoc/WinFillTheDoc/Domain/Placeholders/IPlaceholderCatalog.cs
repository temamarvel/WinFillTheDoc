namespace WinFillTheDoc.Domain.Placeholders;

public interface IPlaceholderCatalog
{
    IReadOnlyList<PlaceholderDefinition> GetAll();
}
