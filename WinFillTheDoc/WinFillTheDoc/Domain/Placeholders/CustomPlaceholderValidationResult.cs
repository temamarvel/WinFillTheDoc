namespace WinFillTheDoc.Domain.Placeholders;

public sealed record CustomPlaceholderValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
    public static CustomPlaceholderValidationResult Success { get; } = new([]);
}
