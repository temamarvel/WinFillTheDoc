using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.Services;

public interface IRequisitesExtractionService
{
    Task<IReadOnlyDictionary<string, string>> ExtractAsync(
        string sourceText,
        IReadOnlyList<PlaceholderDescriptor> schemaDescriptors,
        CancellationToken cancellationToken = default);
}
