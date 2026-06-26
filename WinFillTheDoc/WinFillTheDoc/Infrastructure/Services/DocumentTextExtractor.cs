using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed partial class DocumentTextExtractor : IDocumentTextExtractor
{
    private const int MaxChars = 60_000;

    public async Task<ExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        var notes = new List<string>();
        var errors = new List<string>();
        var fileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
        var method = "failed";
        var needsOcr = false;
        var text = string.Empty;

        try
        {
            text = extension switch
            {
                "txt" => await File.ReadAllTextAsync(filePath, cancellationToken),
                "docx" => ExtractDocx(filePath),
                "pdf" => UnsupportedPdf(notes, out needsOcr),
                _ => Unsupported(extension, errors),
            };

            method = extension switch
            {
                "txt" => "plain-text",
                "docx" => "docx-xml",
                "pdf" => "unsupported-pdf",
                _ => "unsupported",
            };
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        var normalized = Normalize(text);
        if (normalized.Length == 0) notes.Add("Text is empty after extraction.");

        return new ExtractionResult(
            normalized,
            method,
            needsOcr,
            new DocumentExtractionDiagnostics(filePath, extension, fileSize, normalized.Length, notes, errors));
    }

    private static string ExtractDocx(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var entries = archive.Entries
            .Where(x => x.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase) &&
                        x.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var xml = reader.ReadToEnd();
            var withBreaks = BreakTagRegex().Replace(xml, "\n");
            var withoutTags = TagRegex().Replace(withBreaks, " ");
            builder.AppendLine(WebUtility.HtmlDecode(withoutTags));
        }

        return builder.ToString();
    }

    private static string UnsupportedPdf(List<string> notes, out bool needsOcr)
    {
        needsOcr = true;
        notes.Add("PDF extraction is not implemented in this stage. Fill the form manually or use TXT/DOCX requisites.");
        return string.Empty;
    }

    private static string Unsupported(string extension, List<string> errors)
    {
        errors.Add($"Unsupported file extension: {extension}.");
        return string.Empty;
    }

    private static string Normalize(string text)
    {
        var normalized = WhitespaceRegex().Replace(text, " ").Trim();
        return normalized.Length <= MaxChars ? normalized : normalized[..MaxChars];
    }

    [GeneratedRegex("<w:(br|cr|tab)[^>]*>|</w:p>|</w:tr>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTagRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
