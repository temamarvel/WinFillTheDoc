using FillTheDoc.OpenAIClient.Models;

namespace FillTheDoc.OpenAIClient.Abstractions;

public interface IOpenAIClient {
    Task<OpenAIResult<string>> RequestAsync(
        string system,
        string user,
        double temperature = 0.0,
        CancellationToken cancellationToken = default);

    Task<OpenAIResult<T>> RequestJsonAsync<T>(
        string system,
        string user,
        double temperature = 0.0,
        CancellationToken cancellationToken = default);
}