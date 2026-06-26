using WinFillTheDoc.Domain.Documents;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.Services;

public sealed class DocumentWorkflowState
{
    public DocumentFile? TemplateFile { get; set; }
    public DocumentFile? SourceFile { get; set; }
    public IReadOnlyList<DocumentFieldValue> FieldValues { get; set; } = [];
    public IReadOnlyDictionary<string, string> ExtractedValues { get; set; } = new Dictionary<string, string>();
    public string? ExtractionStatusMessage { get; set; }
    public TemplateInspection? TemplateInspection { get; set; }
    public IReadOnlyDictionary<string, string> ResolvedValues { get; set; } = new Dictionary<string, string>();
}
