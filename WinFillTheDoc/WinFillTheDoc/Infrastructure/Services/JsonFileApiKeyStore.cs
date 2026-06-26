using System.IO;
using System.Text.Json;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class JsonFileApiKeyStore : IApiKeyStore, IDaDataTokenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public JsonFileApiKeyStore() : this(GetDefaultSettingsPath())
    {
    }

    public JsonFileApiKeyStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(GetApiKey());
    public bool HasToken => !string.IsNullOrWhiteSpace(GetToken());

    public string? GetApiKey()
    {
        if (!File.Exists(_settingsPath)) return null;

        try
        {
            return ReadSettings().OpenAI?.ApiKey;
        }
        catch
        {
            return null;
        }
    }

    public string? GetToken()
    {
        if (!File.Exists(_settingsPath)) return null;

        try
        {
            return ReadSettings().DaData?.ApiKey;
        }
        catch
        {
            return null;
        }
    }

    public void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var settings = ReadSettings() with { OpenAI = new ApiKeySettings(apiKey.Trim()) };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public void SaveToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var settings = ReadSettings() with { DaData = new ApiKeySettings(token.Trim()) };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WinFillTheDoc", "settings.json");
    }

    private SettingsFile ReadSettings()
    {
        if (!File.Exists(_settingsPath)) return new SettingsFile(null, null);

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<SettingsFile>(json) ?? new SettingsFile(null, null);
    }

    private sealed record SettingsFile(ApiKeySettings? OpenAI, ApiKeySettings? DaData);
    private sealed record ApiKeySettings(string? ApiKey);
}
