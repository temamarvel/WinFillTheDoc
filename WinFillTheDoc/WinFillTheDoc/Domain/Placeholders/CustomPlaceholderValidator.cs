using System.Text.RegularExpressions;

namespace WinFillTheDoc.Domain.Placeholders;

public sealed partial class CustomPlaceholderValidator
{
    public CustomPlaceholderValidationResult Validate(
        CustomPlaceholderDefinition definition,
        IEnumerable<string> existingKeys,
        string? originalKey = null)
    {
        var errors = new List<string>();
        var key = definition.Key.Trim();

        if (string.IsNullOrWhiteSpace(key))
            errors.Add("Ключ не может быть пустым.");
        else if (!PlaceholderKeyRegex().IsMatch(key))
            errors.Add("Ключ должен содержать только lowercase latin, цифры и underscore.");

        if (existingKeys.Any(existing => !string.Equals(existing, originalKey, StringComparison.Ordinal) &&
                                         string.Equals(existing, key, StringComparison.Ordinal)))
            errors.Add("Плейсхолдер с таким ключом уже существует.");

        if (string.IsNullOrWhiteSpace(definition.Title))
            errors.Add("Название не может быть пустым.");

        if (definition.InputKind == PlaceholderInputKind.Choice &&
            definition.Options.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Count() == 0)
            errors.Add("Для choice-поля нужен хотя бы один вариант.");

        return errors.Count == 0 ? CustomPlaceholderValidationResult.Success : new CustomPlaceholderValidationResult(errors);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex PlaceholderKeyRegex();
}
