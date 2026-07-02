using NUnit.Framework;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Tests;

public sealed class CompanyReferenceValidatorTests
{
    [Test]
    public async Task ResolveAsync_ReturnsEmpty_WhenDaDataTokenMissing()
    {
        var validator = new CompanyReferenceValidator(
            new FakeCompanyReferenceService(new CompanyReference(new Dictionary<string, string>
            {
                ["inn"] = "7701234567",
            })),
            new FakeDaDataTokenStore(null));

        var resolution = await validator.ResolveAsync(new Dictionary<string, string>
        {
            ["inn"] = "7701234567",
        });

        Assert.That(resolution.Issues, Is.Empty);
        Assert.That(resolution.ReferenceValues, Is.Empty);
    }

    [Test]
    public async Task ResolveAsync_ReturnsIssues_WhenFieldsDifferFromReference()
    {
        var validator = new CompanyReferenceValidator(
            new FakeCompanyReferenceService(new CompanyReference(new Dictionary<string, string>
            {
                ["inn"] = "7701234567",
                ["company_name"] = "Ромашка",
                ["legal_form"] = "ООО",
                ["address"] = "г Москва, ул Ленина, д 1",
            })),
            new FakeDaDataTokenStore("token"));

        var resolution = await validator.ResolveAsync(new Dictionary<string, string>
        {
            ["inn"] = "7701234567",
            ["company_name"] = "Василек",
            ["legal_form"] = "ИП",
            ["address"] = "г Санкт-Петербург, Невский проспект, д 10",
        });

        Assert.That(resolution.ReferenceValues["company_name"], Is.EqualTo("Ромашка"));
        Assert.That(resolution.Issues.Keys, Is.EquivalentTo(new[] { "company_name", "legal_form", "address" }));
    }

    [Test]
    public async Task ResolveAsync_DoesNotReturnIssue_ForSimilarCompanyName()
    {
        var validator = new CompanyReferenceValidator(
            new FakeCompanyReferenceService(new CompanyReference(new Dictionary<string, string>
            {
                ["inn"] = "7701234567",
                ["company_name"] = "Ромашка",
            })),
            new FakeDaDataTokenStore("token"));

        var resolution = await validator.ResolveAsync(new Dictionary<string, string>
        {
            ["inn"] = "7701234567",
            ["company_name"] = "ООО Ромашка",
        });

        Assert.That(resolution.Issues, Does.Not.ContainKey("company_name"));
    }

    private sealed class FakeCompanyReferenceService : ICompanyReferenceService
    {
        private readonly CompanyReference? _reference;

        public FakeCompanyReferenceService(CompanyReference? reference)
        {
            _reference = reference;
        }

        public Task<CompanyReference?> FindAsync(string innOrOgrn, CancellationToken cancellationToken = default) =>
            Task.FromResult(_reference);
    }

    private sealed class FakeDaDataTokenStore : IDaDataTokenStore
    {
        private readonly string? _token;

        public FakeDaDataTokenStore(string? token)
        {
            _token = token;
        }

        public bool HasToken => !string.IsNullOrWhiteSpace(_token);
        public string? GetToken() => _token;
        public void SaveToken(string token) => throw new NotSupportedException();
        public void DeleteToken() => throw new NotSupportedException();
    }
}
