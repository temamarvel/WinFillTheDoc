using FillTheDoc.OpenAIClient.Abstractions;

namespace FillTheDoc.OpenAIClient;

public sealed class StaticOpenAIApiKeyProvider : IOpenAIApiKeyProvider {
    private readonly string _apiKey;

    public StaticOpenAIApiKeyProvider(string apiKey) {
        _apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("API key is empty.", nameof(apiKey))
            : apiKey;
    }

    public ValueTask<string> GetApiKeyAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_apiKey);
}