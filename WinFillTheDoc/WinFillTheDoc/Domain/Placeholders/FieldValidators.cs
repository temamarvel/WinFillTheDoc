using System.Net.Mail;

namespace WinFillTheDoc.Domain.Placeholders;

public static class FieldValidators
{
    public static FieldIssue? NonEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? FieldIssue.Error("Поле не может быть пустым.") : null;

    public static FieldIssue? Inn(string value)
    {
        if (value.Length == 10 && value.All(char.IsDigit))
        {
            var weights = new[] { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
            return CheckDigit(value, weights, 9, 9) ? null : FieldIssue.Error("Некорректный ИНН.");
        }

        if (value.Length == 12 && value.All(char.IsDigit))
        {
            var first = new[] { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8, 0 };
            var second = new[] { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8, 0 };
            return CheckDigit(value, first, 11, 10) && CheckDigit(value, second, 11, 11)
                ? null : FieldIssue.Error("Некорректный ИНН.");
        }

        return FieldIssue.Error("ИНН должен содержать 10 или 12 цифр.");
    }

    public static FieldIssue? Ogrn(string value)
    {
        if (!value.All(char.IsDigit) || (value.Length != 13 && value.Length != 15))
            return FieldIssue.Error("ОГРН должен содержать 13 или 15 цифр.");

        var divisor = value.Length == 13 ? 11 : 13;
        var number = long.Parse(value[..^1]);
        return (int)((number % divisor) % 10) == value[^1] - '0'
            ? null : FieldIssue.Error("Некорректный ОГРН.");
    }

    public static FieldIssue? Kpp(string value) =>
        value.Length == 9 && value.All(char.IsDigit) ? null : FieldIssue.Warning("КПП должен содержать 9 цифр.");

    public static FieldIssue? Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var address = new MailAddress(value);
            return address.Address == value ? null : FieldIssue.Error("Некорректный email.");
        }
        catch (FormatException) { return FieldIssue.Error("Некректный email."); }
    }

    public static FieldIssue? FullName(string value) => string.IsNullOrWhiteSpace(value)
        ? FieldIssue.Error("Поле не может быть пустым.")
        : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2
            ? FieldIssue.Warning("Ожидаются как минимум фамилия и имя.") : null;

    public static FieldIssue? ShortName(string value) => string.IsNullOrWhiteSpace(value)
        ? FieldIssue.Error("Поле не может быть пустым.")
        : !value.Contains('.') ? FieldIssue.Warning("Ожидается формат с инициалами, например «Иванов И.И.».") : null;

    public static FieldIssue? Address(string value) => string.IsNullOrWhiteSpace(value) || value.Length >= 10
        ? null : FieldIssue.Warning("Адрес выглядит слишком коротким.");

    public static FieldIssue? Phone(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = value.Count(char.IsDigit);
        return digits is >= 10 and <= 15 ? null : FieldIssue.Warning("Телефон обычно содержит от 10 до 15 цифр.");
    }

    public static FieldIssue? DecimalInRange(string value, decimal min, decimal max)
    {
        if (!decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var number)) return FieldIssue.Error("Введите число.");
        return number < min || number > max ? FieldIssue.Error($"Значение должно быть от {min} до {max}.") : null;
    }

    public static FieldIssue? Choice(string value, ChoiceInputConfiguration configuration) =>
        string.IsNullOrWhiteSpace(value) && !configuration.AllowsEmptyValue ? FieldIssue.Error("Выберите значение.")
        : !string.IsNullOrWhiteSpace(value) && !configuration.Options.Contains(value) ? FieldIssue.Error("Выбрано неизвестное значение.") : null;

    private static bool CheckDigit(string value, IReadOnlyList<int> weights, int digitCount, int checkIndex)
    {
        var sum = 0;
        for (var index = 0; index < digitCount; index++) sum += (value[index] - '0') * weights[index];
        return (sum % 11) % 10 == value[checkIndex] - '0';
    }
}
