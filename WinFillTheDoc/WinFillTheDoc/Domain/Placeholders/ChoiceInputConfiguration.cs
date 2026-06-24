namespace WinFillTheDoc.Domain.Placeholders;

public sealed record ChoiceInputConfiguration(
    IReadOnlyList<string> Options,
    bool AllowsEmptyValue = false,
    string EmptyTitle = "Не выбрано");
