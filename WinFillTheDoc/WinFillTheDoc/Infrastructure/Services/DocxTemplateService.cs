using DocUtils;
using System.IO;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class DocxTemplateService : IDocxTemplateService
{
    private readonly DocxTemplateScanner _scanner = new();
    private readonly DocxTemplateConditionalAssembler _conditionalAssembler = new();
    private readonly DocxTemplateFiller _filler = new();

    public TemplateInspection Inspect(string templatePath, IReadOnlySet<string> knownKeys)
    {
        var report = _scanner.Scan(templatePath);
        var unknown = report.OrderedKeys.Where(key => !knownKeys.Contains(key)).ToArray();
        var issues = report.Issues.Select(issue => $"{issue.PartPath}: {issue.Message}").ToArray();
        return new TemplateInspection(report.OrderedKeys, unknown, issues);
    }

    public DocumentGenerationResult Generate(string templatePath, string outputPath, IReadOnlyDictionary<string, string> values)
    {
        var assemblyPath = Path.Combine(Path.GetTempPath(), $"FillTheDoc_assembly_{Guid.NewGuid():N}.docx");
        var filledPath = Path.Combine(Path.GetTempPath(), $"FillTheDoc_filled_{Guid.NewGuid():N}.docx");
        try
        {
            _conditionalAssembler.Assemble(templatePath, assemblyPath, values);
            var report = _filler.Fill(assemblyPath, filledPath, values, new DocxFillOptions
            {
                MissingKeyPolicy = MissingKeyPolicy.Error,
                PartFailurePolicy = PartFailurePolicy.FailFast,
                ReplacementStylePolicy = ReplacementStylePolicy.RemoveHighlightOnly,
            });

            File.Move(filledPath, outputPath, true);
            return new DocumentGenerationResult(outputPath, report.ReplacementsCount, report.ProcessedParts);
        }
        finally
        {
            if (File.Exists(assemblyPath)) File.Delete(assemblyPath);
            if (File.Exists(filledPath)) File.Delete(filledPath);
        }
    }
}
