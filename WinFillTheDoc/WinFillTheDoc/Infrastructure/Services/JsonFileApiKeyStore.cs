using System.IO;
using System.Text.Json;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class JsonFileApiKeyStore : IApiKeyStore
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

    public string? GetApiKey()
    {
        if (!File.Exists(_settingsPath)) return null;

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsFile>(json)?.OpenAI?.ApiKey;
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

        var settings = new SettingsFile(new OpenAISettings(apiKey.Trim()));
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WinFillTheDoc", "settings.json");
    }

    private sealed record SettingsFile(OpenAISettings? OpenAI);
    private sealed record OpenAISettings(string? ApiKey);
}
