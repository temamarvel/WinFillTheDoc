using System.Text.RegularExpressions;

namespace WinFillTheDoc.Domain.Placeholders;

public static class FieldNormalizers
{
    public static string Trim(string value) => value.Trim();

    public static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());

    public static string Phone(string value)
    {
        var trimmed = value.Trim().Replace('–', '-').Replace("(", string.Empty).Replace(")", string.Empty);
        return Regex.Replace(trimmed, @"\s+", string.Empty);
    }
}
