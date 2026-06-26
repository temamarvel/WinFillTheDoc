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
}
