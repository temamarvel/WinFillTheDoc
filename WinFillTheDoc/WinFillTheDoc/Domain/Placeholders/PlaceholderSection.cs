namespace WinFillTheDoc.Domain.Placeholders;

public enum PlaceholderSection
{
    Company,
    Document,
    Computed,
    Custom,
}

public static class PlaceholderSectionExtensions
{
    public static string GetTitle(this PlaceholderSection section) => section switch
    {
        PlaceholderSection.Company => "Реквизиты компании",
        PlaceholderSection.Document => "Данные документа",
        PlaceholderSection.Computed => "Вычисляемые значения",
        PlaceholderSection.Custom => "Пользовательские поля",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };
}
