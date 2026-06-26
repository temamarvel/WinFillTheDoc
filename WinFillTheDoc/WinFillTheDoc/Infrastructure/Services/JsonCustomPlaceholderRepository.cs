using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class JsonCustomPlaceholderRepository : ICustomPlaceholderRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _filePath;
    private readonly List<CustomPlaceholderDefinition> _definitions;

    public JsonCustomPlaceholderRepository() : this(GetDefaultPath())
    {
    }

    public JsonCustomPlaceholderRepository(string filePath)
    {
        _filePath = filePath;
        _definitions = Load();
    }

    public IReadOnlyList<CustomPlaceholderDefinition> GetAll() =>
        _definitions.OrderBy(x => x.Section).ThenBy(x => x.Key).ToList();

    public void Add(CustomPlaceholderDefinition definition)
    {
        _definitions.Add(Normalize(definition));
        Save();
    }

    public void Update(string originalKey, CustomPlaceholderDefinition definition)
    {
        var index = _definitions.FindIndex(x => string.Equals(x.Key, originalKey, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("Custom placeholder was not found.");

        _definitions[index] = Normalize(definition);
        Save();
    }

    public void Delete(string key)
    {
        _definitions.RemoveAll(x => string.Equals(x.Key, key, StringComparison.Ordinal));
        Save();
    }

    private List<CustomPlaceholderDefinition> Load()
    {
        if (!File.Exists(_filePath)) return [];

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<CustomPlaceholderDefinition>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(_definitions, JsonOptions));
    }

    private static CustomPlaceholderDefinition Normalize(CustomPlaceholderDefinition definition) =>
        definition with
        {
            Key = definition.Key.Trim(),
            Title = definition.Title.Trim(),
            Description = definition.Description.Trim(),
            Options = definition.Options.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).ToList(),
        };

    private static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WinFillTheDoc", "custom-placeholders.json");
    }
}
