namespace WinFillTheDoc.Application.Services;

public interface IDocxTemplateService
{
    TemplateInspection Inspect(string templatePath, IReadOnlySet<string> knownKeys);
    DocumentGenerationResult Generate(string templatePath, string outputPath, IReadOnlyDictionary<string, string> values);
}
