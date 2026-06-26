namespace WinFillTheDoc.Application.Services;

public sealed record ExtractionResult(
    string Text,
    string Method,
    bool NeedsOcr,
    DocumentExtractionDiagnostics Diagnostics);

public sealed record DocumentExtractionDiagnostics(
    string OriginalPath,
    string Extension,
    long FileSizeBytes,
    int ProducedChars,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Errors);
