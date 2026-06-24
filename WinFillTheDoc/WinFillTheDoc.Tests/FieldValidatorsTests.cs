using WinFillTheDoc.Domain.Placeholders;
using NUnit.Framework;

namespace WinFillTheDoc.Tests;

public sealed class FieldValidatorsTests
{
    [TestCase("7707083893")]
    [TestCase("500100732259")]
    public void Inn_AcceptsValidChecksum(string value)
    {
        Assert.That(FieldValidators.Inn(value), Is.Null);
    }

    [TestCase("7707083894")]
    [TestCase("123")]
    public void Inn_RejectsInvalidValue(string value)
    {
        Assert.That(FieldValidators.Inn(value)?.Severity, Is.EqualTo(FieldIssueSeverity.Error));
    }

    [TestCase("1027700132195")]
    [TestCase("304500116000157")]
    public void Ogrn_AcceptsValidChecksum(string value)
    {
        Assert.That(FieldValidators.Ogrn(value), Is.Null);
    }

    [Test]
    public void Commission_RejectsValueOutsideConfiguredRange()
    {
        Assert.That(FieldValidators.DecimalInRange("101", 0, 100)?.Severity, Is.EqualTo(FieldIssueSeverity.Error));
        Assert.That(FieldValidators.DecimalInRange("1,5", 0, 100), Is.Null);
    }

    [Test]
    public void PaymentMethod_RequiresASelection()
    {
        var choices = new ChoiceInputConfiguration(["счет", "сбп"]);
        Assert.That(FieldValidators.Choice(string.Empty, choices)?.Severity, Is.EqualTo(FieldIssueSeverity.Error));
        Assert.That(FieldValidators.Choice("сбп", choices), Is.Null);
    }
}
