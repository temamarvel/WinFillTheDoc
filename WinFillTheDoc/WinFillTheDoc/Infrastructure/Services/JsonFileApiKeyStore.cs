using System.IO;
using System.Text.Json;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class JsonFileApiKeyStore : IApiKeyStore, IDaDataTokenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    private readonly ISecretProtector _secretProtector;

    public JsonFileApiKeyStore() : this(GetDefaultSettingsPath(), new DpapiSecretProtector())
    {
    }

    public JsonFileApiKeyStore(string settingsPath) : this(settingsPath, new DpapiSecretProtector())
    {
    }

    public JsonFileApiKeyStore(string settingsPath, ISecretProtector secretProtector)
    {
        _settingsPath = settingsPath;
        _secretProtector = secretProtector;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(GetApiKey());
    public bool HasToken => !string.IsNullOrWhiteSpace(GetToken());

    public string? GetApiKey()
    {
        try
        {
            var settings = ReadSettings();
            var value = ReadSecret(settings.OpenAI);
            if (settings.OpenAI?.ApiKey is not null)
                WriteSettings(settings with { OpenAI = value is null ? null : new ApiKeySettings(null, _secretProtector.Protect(value)) });
            return value;
        }
        catch
        {
            return null;
        }
    }

    public string? GetToken()
    {
        try
        {
            var settings = ReadSettings();
            var value = ReadSecret(settings.DaData);
            if (settings.DaData?.ApiKey is not null)
                WriteSettings(settings with { DaData = value is null ? null : new ApiKeySettings(null, _secretProtector.Protect(value)) });
            return value;
        }
        catch
        {
            return null;
        }
    }

    public void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        var settings = ReadSettings() with { OpenAI = new ApiKeySettings(null, _secretProtector.Protect(apiKey.Trim())) };
        WriteSettings(settings);
    }

    public void SaveToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var settings = ReadSettings() with { DaData = new ApiKeySettings(null, _secretProtector.Protect(token.Trim())) };
        WriteSettings(settings);
    }

    public void DeleteApiKey()
    {
        var settings = ReadSettings() with { OpenAI = null };
        WriteSettings(settings);
    }

    public void DeleteToken()
    {
        var settings = ReadSettings() with { DaData = null };
        WriteSettings(settings);
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

    private void WriteSettings(SettingsFile settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private string? ReadSecret(ApiKeySettings? settings)
    {
        if (!string.IsNullOrWhiteSpace(settings?.ProtectedApiKey))
            return _secretProtector.Unprotect(settings.ProtectedApiKey);

        return string.IsNullOrWhiteSpace(settings?.ApiKey) ? null : settings.ApiKey.Trim();
    }

    private sealed record SettingsFile(ApiKeySettings? OpenAI, ApiKeySettings? DaData);
    private sealed record ApiKeySettings(string? ApiKey, string? ProtectedApiKey);
}
