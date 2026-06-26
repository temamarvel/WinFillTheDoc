namespace WinFillTheDoc.Application.Services;

public interface IDaDataTokenStore
{
    bool HasToken { get; }
    string? GetToken();
    void SaveToken(string token);
}
