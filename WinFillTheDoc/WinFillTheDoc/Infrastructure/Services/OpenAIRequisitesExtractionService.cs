using FillTheDoc.OpenAIClient.Abstractions;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class OpenAIRequisitesExtractionService : IRequisitesExtractionService
{
    private readonly IOpenAIClient _openAIClient;

    public OpenAIRequisitesExtractionService(IOpenAIClient openAIClient)
    {
        _openAIClient = openAIClient;
    }

    public async Task<IReadOnlyDictionary<string, string>> ExtractAsync(
        string sourceText,
        IReadOnlyList<PlaceholderDescriptor> schemaDescriptors,
        CancellationToken cancellationToken = default)
    {
        var system = PromptBuilder.BuildSystem(schemaDescriptors);
        var user = PromptBuilder.BuildUser(sourceText);
        var result = await _openAIClient
            .RequestJsonAsync<Dictionary<string, string?>>(system, user, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.Value
            .Where(x => schemaDescriptors.Any(d => d.Key == x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value!.Trim(), StringComparer.Ordinal);
    }
}
