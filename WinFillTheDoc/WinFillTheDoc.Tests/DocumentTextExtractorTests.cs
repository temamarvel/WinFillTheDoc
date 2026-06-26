using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc.Tests;

public sealed class DocumentTextExtractorTests
{
    [Test]
    public async Task ExtractAsync_ReadsPlainText()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "  ООО Ромашка\r\nИНН 7701234567  ");

        var result = await new DocumentTextExtractor().ExtractAsync(path);

        Assert.That(result.Text, Is.EqualTo("ООО Ромашка ИНН 7701234567"));
        Assert.That(result.Method, Is.EqualTo("plain-text"));
    }

    [Test]
    public async Task ExtractAsync_ReadsMinimalDocxText()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}.docx");
        CreateDocx(path, "ООО Ромашка");

        var result = await new DocumentTextExtractor().ExtractAsync(path);

        Assert.That(result.Text, Does.Contain("ООО Ромашка"));
        Assert.That(result.Method, Is.EqualTo("docx-xml"));
    }

    [Test]
    public async Task ExtractAsync_ReadsTextPdf()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}.pdf");
        CreatePdf(path, "OOO Romashka INN 7701234567");

        var result = await new DocumentTextExtractor().ExtractAsync(path);

        Assert.That(result.Text, Does.Contain("OOO Romashka"));
        Assert.That(result.NeedsOcr, Is.False);
        Assert.That(result.Method, Is.EqualTo("pdf-text-layer"));
    }

    [Test]
    public async Task ExtractAsync_UnsupportedExtension_ReturnsDiagnosticError()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}.rtf");
        await File.WriteAllTextAsync(path, "test");

        var result = await new DocumentTextExtractor().ExtractAsync(path);

        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Diagnostics.Errors.Single(), Does.Contain("Unsupported file extension"));
    }

    private static void CreateDocx(string path, string text)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("word/document.xml");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write($"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
          <w:body><w:p><w:r><w:t>{System.Security.SecurityElement.Escape(text)}</w:t></w:r></w:p></w:body>
        </w:document>
        """);
    }

    private static void CreatePdf(string path, string text)
    {
        var escapedText = text.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)");
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount($"BT /F1 24 Tf 72 720 Td ({escapedText}) Tj ET")} >>\nstream\nBT /F1 24 Tf 72 720 Td ({escapedText}) Tj ET\nendstream",
        };

        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            writer.Write($"{offset:0000000000} 00000 n \n");

        writer.Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
    }
}
