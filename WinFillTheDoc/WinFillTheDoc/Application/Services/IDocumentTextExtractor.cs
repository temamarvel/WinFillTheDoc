namespace WinFillTheDoc.Application.Services;

public interface IDocumentTextExtractor
{
    Task<ExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}
