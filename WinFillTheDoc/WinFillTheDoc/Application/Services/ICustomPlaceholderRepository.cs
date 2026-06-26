using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.Services;

public interface ICustomPlaceholderRepository
{
    IReadOnlyList<CustomPlaceholderDefinition> GetAll();
    void Add(CustomPlaceholderDefinition definition);
    void Update(string originalKey, CustomPlaceholderDefinition definition);
    void Delete(string key);
}
