namespace WinFillTheDoc.Domain.Placeholders;

public sealed record FieldIssue(FieldIssueSeverity Severity, string Text)
{
    public static FieldIssue Error(string text) => new(FieldIssueSeverity.Error, text);
    public static FieldIssue Warning(string text) => new(FieldIssueSeverity.Warning, text);
}

public enum FieldIssueSeverity
{
    Warning,
    Error,
}
