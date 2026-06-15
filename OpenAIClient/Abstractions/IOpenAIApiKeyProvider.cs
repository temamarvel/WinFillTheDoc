namespace FillTheDoc.OpenAIClient.Abstractions;

public interface IOpenAIApiKeyProvider {
    ValueTask<string> GetApiKeyAsync(CancellationToken cancellationToken = default);
}