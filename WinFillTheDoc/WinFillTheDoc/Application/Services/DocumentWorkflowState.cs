using WinFillTheDoc.Domain.Documents;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.Services;

public sealed class DocumentWorkflowState
{
    public DocumentFile? TemplateFile { get; set; }
    public DocumentFile? SourceFile { get; set; }
    public IReadOnlyList<DocumentFieldValue> FieldValues { get; set; } = [];
}
