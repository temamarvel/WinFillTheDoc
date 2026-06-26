namespace WinFillTheDoc.Application.Services;

public interface IDocumentDataCopyStringBuilder
{
    string BuildRow(IReadOnlyDictionary<string, string> resolvedValues);
}
