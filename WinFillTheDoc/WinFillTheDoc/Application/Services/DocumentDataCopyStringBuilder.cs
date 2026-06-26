namespace WinFillTheDoc.Application.Services;

public sealed class DocumentDataCopyStringBuilder : IDocumentDataCopyStringBuilder
{
    private static readonly string[] ColumnKeys =
    [
        "full_company_name",
        "ceo_full_name",
        "inn",
        "phone",
        "email",
        "document_number",
        "date_short",
        "",
        "fee",
        "min_fee",
        "",
        "",
        "",
    ];

    public string BuildRow(IReadOnlyDictionary<string, string> resolvedValues) =>
        string.Join('\t', ColumnKeys.Select(key => key.Length == 0 ? string.Empty : Sanitize(GetValue(resolvedValues, key))));

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : string.Empty;

    private static string Sanitize(string value) =>
        value.Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
}
