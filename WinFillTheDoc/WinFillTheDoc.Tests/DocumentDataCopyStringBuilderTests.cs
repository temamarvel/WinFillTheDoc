using NUnit.Framework;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Tests;

public sealed class DocumentDataCopyStringBuilderTests
{
    [Test]
    public void BuildRow_ReturnsFixedThirteenColumnTsv()
    {
        var values = new Dictionary<string, string>
        {
            ["full_company_name"] = "ООО Ромашка",
            ["ceo_full_name"] = "Иванов Иван Иванович",
            ["inn"] = "7701234567",
            ["phone"] = "+7 999 000-00-00",
            ["email"] = "info@example.test",
            ["document_number"] = "42",
            ["date_short"] = "26.06.2026",
            ["fee"] = "10",
            ["min_fee"] = "100",
        };

        var row = new DocumentDataCopyStringBuilder().BuildRow(values);
        var columns = row.Split('\t');

        Assert.That(columns, Has.Length.EqualTo(13));
        Assert.That(columns[0], Is.EqualTo("ООО Ромашка"));
        Assert.That(columns[6], Is.EqualTo("26.06.2026"));
        Assert.That(columns[7], Is.Empty);
        Assert.That(columns[10], Is.Empty);
        Assert.That(columns[11], Is.Empty);
        Assert.That(columns[12], Is.Empty);
    }

    [Test]
    public void BuildRow_SanitizesTabsAndLineBreaks()
    {
        var row = new DocumentDataCopyStringBuilder().BuildRow(new Dictionary<string, string>
        {
            ["full_company_name"] = "ООО\tРомашка\r\nТест",
        });

        Assert.That(row.Split('\t')[0], Is.EqualTo("ООО Ромашка  Тест"));
    }

    [Test]
    public void BuildRow_PreservesMissingValuesAsEmptyColumns()
    {
        var row = new DocumentDataCopyStringBuilder().BuildRow(new Dictionary<string, string>());
        var columns = row.Split('\t');

        Assert.That(columns, Has.Length.EqualTo(13));
        Assert.That(columns.All(string.IsNullOrEmpty), Is.True);
    }
}
