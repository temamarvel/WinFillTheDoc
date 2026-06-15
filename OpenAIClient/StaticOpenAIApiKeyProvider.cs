using FillTheDoc.OpenAIClient.Abstractions;

namespace FillTheDoc.OpenAIClient;

public sealed class StaticOpenAIApiKeyProvider : IOpenAIApiKeyProvider {
    private readonly string apiKey;

    public StaticOpenAIApiKeyProvider(string apiKey) {
        this.apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("API key is empty.", nameof(apiKey))
            : apiKey;
    }

    public ValueTask<string> GetApiKeyAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(apiKey);
}