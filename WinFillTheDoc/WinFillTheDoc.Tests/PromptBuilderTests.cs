using NUnit.Framework;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Tests;

public sealed class PromptBuilderTests
{
    [Test]
    public void BuildSystem_UsesOnlyProvidedSchemaKeys()
    {
        var descriptors = new[]
        {
            new PlaceholderDescriptor("company_name", "Название", "", PlaceholderSection.Company, 10, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true),
            new PlaceholderDescriptor("inn", "ИНН", "", PlaceholderSection.Company, 20, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true),
        };

        var prompt = PromptBuilder.BuildSystem(descriptors);

        Assert.That(prompt, Does.Contain("\"company_name\", \"inn\""));
        Assert.That(prompt, Does.Contain("Do not add any extra keys"));
        Assert.That(prompt, Does.Not.Contain("\"kpp\","));
    }
}
