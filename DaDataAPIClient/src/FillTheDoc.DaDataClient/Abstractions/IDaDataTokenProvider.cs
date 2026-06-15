namespace FillTheDoc.DaDataClient.Abstractions;

public interface IDaDataTokenProvider {
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}