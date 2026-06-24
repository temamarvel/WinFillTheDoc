using NUnit.Framework;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Tests;

public sealed class PlaceholderValueAssemblerTests
{
    [Test]
    public void Assemble_AddsDerivedValuesUsingRussianDateAndLegalForm()
    {
        var assembler = new PlaceholderValueAssembler(new FixedTimeProvider(new DateTimeOffset(2026, 4, 22, 12, 0, 0, TimeSpan.Zero)));
        var values = assembler.Assemble(
        [
            Field("company_name", "Ромашка"),
            Field("legal_form", "ООО"),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(values["date_short"], Is.EqualTo("22.04.2026"));
            Assert.That(values["date_long"], Is.EqualTo("«22» апреля 2026 г."));
            Assert.That(values["ceo_role"], Is.EqualTo("Генеральный директор"));
            Assert.That(values["full_company_name"], Is.EqualTo("ООО «Ромашка»"));
            Assert.That(values["rules"], Is.EqualTo("Устава"));
        });
    }

    private static DocumentFieldValue Field(string key, string value)
    {
        var field = new DocumentFieldValue(new PlaceholderDescriptor(
            key, key, string.Empty, PlaceholderSection.Company, 0,
            PlaceholderValueSource.Manual, PlaceholderInputKind.Text, false));
        field.Value = value;
        return field;
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
