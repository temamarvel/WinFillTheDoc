namespace WinFillTheDoc.Application.Services;

public sealed record DocumentGenerationResult(
    string OutputPath,
    int ReplacementsCount,
    IReadOnlyList<string> ProcessedParts);
