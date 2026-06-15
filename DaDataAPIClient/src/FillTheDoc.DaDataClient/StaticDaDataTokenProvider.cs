using FillTheDoc.DaDataClient.Abstractions;

namespace FillTheDoc.DaDataClient;

public sealed class StaticDaDataTokenProvider : IDaDataTokenProvider {
    private readonly string token;

    public StaticDaDataTokenProvider(string token) {
        this.token = string.IsNullOrWhiteSpace(token)
            ? throw new ArgumentException("DaData token is empty.", nameof(token))
            : token;
    }

    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) {
        return ValueTask.FromResult(token);
    }
}