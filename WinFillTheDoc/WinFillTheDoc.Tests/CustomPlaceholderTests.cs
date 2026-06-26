using NUnit.Framework;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Domain.Placeholders;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc.Tests;

public sealed class CustomPlaceholderTests
{
    [Test]
    public void Validator_RejectsInvalidKeyAndDuplicate()
    {
        var definition = new CustomPlaceholderDefinition(
            "Bad-Key",
            "Поле",
            "",
            PlaceholderSection.Custom,
            PlaceholderValueSource.Manual,
            PlaceholderInputKind.Text,
            false,
            []);

        var result = new CustomPlaceholderValidator().Validate(definition, ["company_name", "Bad-Key"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("lowercase"));
        Assert.That(result.Errors, Has.Some.Contains("уже существует"));
    }

    [Test]
    public void Repository_SavesAndLoadsDefinitions()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}", "custom-placeholders.json");
        var repository = new JsonCustomPlaceholderRepository(path);
        repository.Add(new CustomPlaceholderDefinition(
            "contract_subject",
            "Предмет договора",
            "Что продаём",
            PlaceholderSection.Custom,
            PlaceholderValueSource.Manual,
            PlaceholderInputKind.Text,
            true,
            []));

        var loaded = new JsonCustomPlaceholderRepository(path).GetAll();

        Assert.That(loaded, Has.Count.EqualTo(1));
        Assert.That(loaded[0].Key, Is.EqualTo("contract_subject"));
        Assert.That(loaded[0].IsRequired, Is.True);
    }

    [Test]
    public void Registry_IncludesCustomInputDescriptorAndChoiceConfiguration()
    {
        var repository = new InMemoryCustomPlaceholderRepository([
            new CustomPlaceholderDefinition(
                "delivery_type",
                "Тип доставки",
                "",
                PlaceholderSection.Custom,
                PlaceholderValueSource.Manual,
                PlaceholderInputKind.Choice,
                true,
                ["курьер", "самовывоз"]),
        ]);
        var registry = new PlaceholderRegistry(repository);

        var descriptor = registry.GetInputDescriptors().Single(x => x.Key == "delivery_type");
        var choice = registry.GetChoiceConfiguration("delivery_type");

        Assert.That(descriptor.Title, Is.EqualTo("Тип доставки"));
        Assert.That(choice?.Options, Is.EquivalentTo(new[] { "курьер", "самовывоз" }));
        Assert.That(registry.GetFieldPolicy("delivery_type").Validate("unknown")?.Severity, Is.EqualTo(FieldIssueSeverity.Error));
    }

    [Test]
    public void PromptBuilder_IncludesCustomExtractedRule()
    {
        var descriptors = new[]
        {
            new PlaceholderDescriptor("custom_fact", "Факт", "Извлеки факт из документа.", PlaceholderSection.Custom, 1, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, false),
        };

        var prompt = PromptBuilder.BuildSystem(descriptors);

        Assert.That(prompt, Does.Contain("\"custom_fact\""));
        Assert.That(prompt, Does.Contain("Извлеки факт из документа."));
    }

    private sealed class InMemoryCustomPlaceholderRepository : ICustomPlaceholderRepository
    {
        private readonly List<CustomPlaceholderDefinition> _definitions;

        public InMemoryCustomPlaceholderRepository(IEnumerable<CustomPlaceholderDefinition> definitions)
        {
            _definitions = definitions.ToList();
        }

        public IReadOnlyList<CustomPlaceholderDefinition> GetAll() => _definitions;
        public void Add(CustomPlaceholderDefinition definition) => _definitions.Add(definition);
        public void Update(string originalKey, CustomPlaceholderDefinition definition)
        {
            var index = _definitions.FindIndex(x => x.Key == originalKey);
            _definitions[index] = definition;
        }
        public void Delete(string key) => _definitions.RemoveAll(x => x.Key == key);
    }
}
