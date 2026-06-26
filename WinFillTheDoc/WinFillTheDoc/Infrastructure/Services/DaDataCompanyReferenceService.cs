using FillTheDoc.DaDataClient.Abstractions;
using FillTheDoc.DaDataClient.Models;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class DaDataCompanyReferenceService : ICompanyReferenceService
{
    private readonly IDaDataClient _client;

    public DaDataCompanyReferenceService(IDaDataClient client)
    {
        _client = client;
    }

    public async Task<CompanyReference?> FindAsync(string innOrOgrn, CancellationToken cancellationToken = default)
    {
        var result = await _client.FetchCompanyInfoFirstAsync(innOrOgrn, cancellationToken).ConfigureAwait(false);
        var company = result.Value?.Data;
        if (company is null) return null;

        var values = Map(company);
        return values.Count == 0 ? null : new CompanyReference(values);
    }

    private static Dictionary<string, string> Map(DaDataCompanyInfo company)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        Add(values, "inn", DigitsOnly(company.Inn));
        Add(values, "kpp", DigitsOnly(company.Kpp));
        Add(values, "ogrn", DigitsOnly(company.Ogrn));
        Add(values, "legal_form", DetectLegalForm(company));
        Add(values, "company_name", CompanyName(company));
        Add(values, "ceo_full_name", company.Management?.Name);
        Add(values, "address", company.Address?.UnrestrictedValue ?? company.Address?.Value);

        return values;
    }

    private static string? CompanyName(DaDataCompanyInfo company)
    {
        var legalForm = DetectLegalForm(company);
        var candidates = new[]
        {
            company.Name?.Short,
            company.Name?.Full,
            company.Name?.ShortWithOpf,
            company.Name?.FullWithOpf,
        };

        foreach (var candidate in candidates)
        {
            var cleaned = StripLegalForm(candidate, legalForm)?.Trim(' ', '"', '\'', '«', '»', '„', '“', '”');
            if (!string.IsNullOrWhiteSpace(cleaned)) return cleaned;
        }

        return null;
    }

    private static string? DetectLegalForm(DaDataCompanyInfo company)
    {
        if (string.Equals(company.Type, "INDIVIDUAL", StringComparison.OrdinalIgnoreCase)) return "ИП";

        var candidates = new[]
        {
            company.Name?.ShortWithOpf,
            company.Name?.FullWithOpf,
            company.Name?.Short,
            company.Name?.Full,
        };

        foreach (var candidate in candidates)
        {
            var normalized = candidate?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) continue;

            if (normalized.StartsWith("ООО ", StringComparison.Ordinal) ||
                normalized.StartsWith("ОБЩЕСТВО С ОГРАНИЧЕННОЙ ОТВЕТСТВЕННОСТЬЮ", StringComparison.Ordinal))
                return "ООО";
            if (normalized.StartsWith("ИП ", StringComparison.Ordinal) ||
                normalized.StartsWith("ИНДИВИДУАЛЬНЫЙ ПРЕДПРИНИМАТЕЛЬ", StringComparison.Ordinal))
                return "ИП";
            if (normalized.StartsWith("ПАО ", StringComparison.Ordinal) ||
                normalized.StartsWith("ПУБЛИЧНОЕ АКЦИОНЕРНОЕ ОБЩЕСТВО", StringComparison.Ordinal))
                return "ПАО";
            if (normalized.StartsWith("АО ", StringComparison.Ordinal) ||
                normalized.StartsWith("АКЦИОНЕРНОЕ ОБЩЕСТВО", StringComparison.Ordinal))
                return "АО";
            if (normalized.StartsWith("ЗАО ", StringComparison.Ordinal) ||
                normalized.StartsWith("ЗАКРЫТОЕ АКЦИОНЕРНОЕ ОБЩЕСТВО", StringComparison.Ordinal))
                return "ЗАО";
        }

        return null;
    }

    private static string? StripLegalForm(string? value, string? legalForm)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (string.IsNullOrWhiteSpace(legalForm)) return value;

        var fullForm = legalForm switch
        {
            "ООО" => "Общество с ограниченной ответственностью",
            "ИП" => "Индивидуальный предприниматель",
            "ПАО" => "Публичное акционерное общество",
            "АО" => "Акционерное общество",
            "ЗАО" => "Закрытое акционерное общество",
            _ => null,
        };

        foreach (var prefix in new[] { fullForm, legalForm }.Where(x => !string.IsNullOrWhiteSpace(x)).OrderByDescending(x => x!.Length))
        {
            if (value.StartsWith(prefix!, StringComparison.OrdinalIgnoreCase))
                return value[prefix!.Length..].Trim();
        }

        return value;
    }

    private static string? DigitsOnly(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray());

    private static void Add(Dictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) values[key] = value.Trim();
    }
}
