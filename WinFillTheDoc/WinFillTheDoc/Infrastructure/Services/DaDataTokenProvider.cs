using FillTheDoc.DaDataClient.Abstractions;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class DaDataTokenProvider : IDaDataTokenProvider
{
    private readonly IDaDataTokenStore _tokenStore;

    public DaDataTokenProvider(IDaDataTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = _tokenStore.GetToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("DaData API token is not configured.");

        return ValueTask.FromResult(token);
    }
}
