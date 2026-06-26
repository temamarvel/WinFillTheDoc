using FillTheDoc.OpenAIClient.Abstractions;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class OpenAIApiKeyProvider : IOpenAIApiKeyProvider
{
    private readonly IApiKeyStore _apiKeyStore;

    public OpenAIApiKeyProvider(IApiKeyStore apiKeyStore)
    {
        _apiKeyStore = apiKeyStore;
    }

    public ValueTask<string> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = _apiKeyStore.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");

        return ValueTask.FromResult(apiKey);
    }
}
