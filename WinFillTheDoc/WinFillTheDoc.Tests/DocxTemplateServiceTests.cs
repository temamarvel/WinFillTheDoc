using System.IO.Compression;
using NUnit.Framework;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc.Tests;

public sealed class DocxTemplateServiceTests
{
    [Test]
    public void Generate_ReplacesTemplateTokenAndProducesScannableDocument()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"FillTheDocTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var templatePath = Path.Combine(directory, "template.docx");
        var outputPath = Path.Combine(directory, "output.docx");

        try
        {
            CreateDocx(templatePath, "<!company_name!>");
            var service = new DocxTemplateService();

            var inspection = service.Inspect(templatePath, new HashSet<string>(["company_name"]));
            var result = service.Generate(templatePath, outputPath, new Dictionary<string, string> { ["company_name"] = "Ромашка" });

            Assert.Multiple(() =>
            {
                Assert.That(inspection.FoundKeys, Is.EqualTo(new[] { "company_name" }));
                Assert.That(inspection.UnknownKeys, Is.Empty);
                Assert.That(result.ReplacementsCount, Is.EqualTo(1));
                Assert.That(File.Exists(outputPath), Is.True);
                Assert.That(ReadDocumentXml(outputPath), Does.Contain("Ромашка"));
                Assert.That(service.Inspect(outputPath, new HashSet<string>(["company_name"])).FoundKeys, Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void CreateDocx(string path, string text)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("word/document.xml");
        using var writer = new StreamWriter(entry.Open());
        var escapedText = System.Security.SecurityElement.Escape(text);
        writer.Write($"<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>{escapedText}</w:t></w:r></w:p></w:body></w:document>");
    }

    private static string ReadDocumentXml(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        using var reader = new StreamReader(archive.GetEntry("word/document.xml")!.Open());
        return reader.ReadToEnd();
    }
}
