namespace WinFillTheDoc.Domain.Placeholders;

public enum PlaceholderValueSource
{
    Extracted,
    Manual,
}

public static class PlaceholderValueSourceExtensions
{
    public static string GetLabel(this PlaceholderValueSource source) => source switch
    {
        PlaceholderValueSource.Extracted => "Извлекается из исходного файла",
        PlaceholderValueSource.Manual => "Заполняется вручную",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };
}
