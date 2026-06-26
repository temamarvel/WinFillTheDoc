using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed record PlaceholderLibraryItem(
    string Key,
    string Title,
    string Description,
    PlaceholderSection Section,
    PlaceholderValueSource? ValueSource,
    PlaceholderInputKind InputKind,
    bool IsRequired,
    IReadOnlyList<string> Options,
    bool IsCustom)
{
    public string Token => $"<!{Key}!>";
    public string KindLabel => IsCustom ? "Пользовательский" : "Встроенный";
    public string SectionLabel => Section.GetTitle();
    public string SourceLabel => ValueSource?.GetLabel() ?? "Вычисляется системой";
}
