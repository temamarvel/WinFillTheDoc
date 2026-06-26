using System.Text.RegularExpressions;

namespace WinFillTheDoc.Application.Services;

public sealed partial class CompanyReferenceValidator : ICompanyReferenceValidator
{
    private readonly ICompanyReferenceService _referenceService;
    private readonly IDaDataTokenStore _tokenStore;
    private readonly Dictionary<string, CompanyReference> _cache = new(StringComparer.Ordinal);

    public CompanyReferenceValidator(ICompanyReferenceService referenceService, IDaDataTokenStore tokenStore)
    {
        _referenceService = referenceService;
        _tokenStore = tokenStore;
    }

    public async Task<CompanyReferenceResolution> ResolveAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        if (!_tokenStore.HasToken) return CompanyReferenceResolution.Empty;

        var lookup = GetTrimmed(values, "ogrn") ?? GetTrimmed(values, "inn");
        if (lookup is null) return CompanyReferenceResolution.Empty;

        var reference = await FindCachedAsync(lookup, cancellationToken).ConfigureAwait(false);
        if (reference is null) return CompanyReferenceResolution.Empty;

        var issues = new Dictionary<string, FieldReferenceIssue>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            if (TryBuildIssue(key, value, reference.Values, out var issue))
                issues[key] = issue;
        }

        return new CompanyReferenceResolution(issues, reference.Values);
    }

    private async Task<CompanyReference?> FindCachedAsync(string lookup, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(lookup, out var cached)) return cached;

        var reference = await _referenceService.FindAsync(lookup, cancellationToken).ConfigureAwait(false);
        if (reference is null) return null;

        foreach (var key in new[] { lookup, GetTrimmed(reference.Values, "inn"), GetTrimmed(reference.Values, "ogrn") })
        {
            if (!string.IsNullOrWhiteSpace(key)) _cache[key] = reference;
        }

        return reference;
    }

    private static bool TryBuildIssue(
        string key,
        string value,
        IReadOnlyDictionary<string, string> referenceValues,
        out FieldReferenceIssue issue)
    {
        issue = null!;
        var current = value.Trim();
        if (current.Length == 0) return false;

        if (!referenceValues.TryGetValue(key, out var reference) || string.IsNullOrWhiteSpace(reference))
            return false;

        var message = key switch
        {
            "inn" when DigitsOnly(current) != DigitsOnly(reference) => "ИНН не совпадает с ФНС.",
            "kpp" when DigitsOnly(current) != DigitsOnly(reference) => "КПП не совпадает с ФНС.",
            "ogrn" when DigitsOnly(current) != DigitsOnly(reference) => "ОГРН/ОГРНИП не совпадает с ФНС.",
            "legal_form" when NormalizeLegalForm(current) != NormalizeLegalForm(reference) => "Правовая форма не совпадает с ФНС.",
            "company_name" when !IsSimilar(current, reference, 0.72) => $"Название не совпадает с ФНС: {reference}.",
            "ceo_full_name" when !IsSimilar(current, reference, 0.72) => $"ФИО руководителя не совпадает с ФНС: {reference}.",
            "address" when !IsSimilar(current, reference, 0.55) => $"Адрес не совпадает с ФНС: {reference}.",
            _ => null,
        };

        if (message is null) return false;

        issue = new FieldReferenceIssue(key, message);
        return true;
    }

    private static string? GetTrimmed(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());

    private static string NormalizeLegalForm(string value) => Normalize(value)
        .Replace("ОБЩЕСТВО С ОГРАНИЧЕННОЙ ОТВЕТСТВЕННОСТЬЮ", "ООО", StringComparison.Ordinal)
        .Replace("ИНДИВИДУАЛЬНЫЙ ПРЕДПРИНИМАТЕЛЬ", "ИП", StringComparison.Ordinal)
        .Replace("ПУБЛИЧНОЕ АКЦИОНЕРНОЕ ОБЩЕСТВО", "ПАО", StringComparison.Ordinal)
        .Replace("АКЦИОНЕРНОЕ ОБЩЕСТВО", "АО", StringComparison.Ordinal)
        .Replace("ЗАКРЫТОЕ АКЦИОНЕРНОЕ ОБЩЕСТВО", "ЗАО", StringComparison.Ordinal);

    private static bool IsSimilar(string value, string reference, double threshold)
    {
        var left = Tokenize(value);
        var right = Tokenize(reference);
        if (left.Count == 0 || right.Count == 0) return false;
        if (left.SetEquals(right) || left.IsSubsetOf(right) || right.IsSubsetOf(left)) return true;

        var intersection = left.Intersect(right).Count();
        var union = left.Union(right).Count();
        return union > 0 && (double)intersection / union >= threshold;
    }

    private static HashSet<string> Tokenize(string value) =>
        Normalize(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length > 1)
            .ToHashSet(StringComparer.Ordinal);

    private static string Normalize(string value) =>
        NonWordRegex().Replace(value.ToUpperInvariant(), " ").Trim();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonWordRegex();
}
