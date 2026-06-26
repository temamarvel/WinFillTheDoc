namespace WinFillTheDoc.Domain.Placeholders;

public sealed record CustomPlaceholderDefinition(
    string Key,
    string Title,
    string Description,
    PlaceholderSection Section,
    PlaceholderValueSource ValueSource,
    PlaceholderInputKind InputKind,
    bool IsRequired,
    IReadOnlyList<string> Options);
