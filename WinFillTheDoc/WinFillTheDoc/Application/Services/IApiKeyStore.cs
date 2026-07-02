namespace WinFillTheDoc.Application.Services;

public interface IApiKeyStore
{
    bool HasApiKey { get; }
    string? GetApiKey();
    void SaveApiKey(string apiKey);
    void DeleteApiKey();
}
