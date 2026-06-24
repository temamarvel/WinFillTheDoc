namespace WinFillTheDoc.Application.Services;

public sealed record TemplateInspection(
    IReadOnlyList<string> FoundKeys,
    IReadOnlyList<string> UnknownKeys,
    IReadOnlyList<string> ProcessingIssues)
{
    public bool HasPlaceholders => FoundKeys.Count > 0;
    public bool CanGenerate => UnknownKeys.Count == 0 && ProcessingIssues.Count == 0;
}
