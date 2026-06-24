namespace WinFillTheDoc.Domain.Placeholders;

public sealed record PlaceholderDescriptor(
    string Key,
    string Title,
    string Description,
    PlaceholderSection Section,
    int Order,
    PlaceholderValueSource? ValueSource,
    PlaceholderInputKind InputKind,
    bool IsRequired,
    string? ExampleValue = null)
{
    public bool AcceptsUserInput => ValueSource is not null;
    public string Token => $"<!{Key}!>";
}
