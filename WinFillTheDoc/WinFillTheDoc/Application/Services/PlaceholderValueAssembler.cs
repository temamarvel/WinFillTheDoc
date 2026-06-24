using System.Globalization;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.Services;

public sealed class PlaceholderValueAssembler
{
    private readonly TimeProvider _timeProvider;

    public PlaceholderValueAssembler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public IReadOnlyDictionary<string, string> Assemble(IEnumerable<DocumentFieldValue> fieldValues)
    {
        var values = fieldValues.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var legalForm = LegalFormInfo.Parse(values.GetValueOrDefault("legal_form"));
        var companyName = values.GetValueOrDefault("company_name", string.Empty).Trim();
        var date = _timeProvider.GetLocalNow();
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        values["date_short"] = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        values["date_long"] = date.ToString("«dd» MMMM yyyy 'г.'", culture);
        values["ceo_role"] = legalForm.IsIndividualEntrepreneur
            ? "Индивидуальный предприниматель"
            : "Генеральный директор";
        values["full_company_name"] = legalForm.MakeShortCompanyName(companyName);
        values["full_company_name_expanded"] = legalForm.MakeFullCompanyName(companyName);
        values["rules"] = legalForm.IsIndividualEntrepreneur
            ? "Листа записи в Едином государственном реестре индивидуальных предпринимателей (ЕГРИП)"
            : "Устава";
        return values;
    }

    private sealed record LegalFormInfo(string ShortName, string FullName, bool IsIndividualEntrepreneur)
    {
        public static LegalFormInfo Parse(string? value)
        {
            var normalized = value?.Trim().Replace(".", string.Empty).ToUpperInvariant();
            return normalized switch
            {
                "ИП" => new("ИП", "Индивидуальный предприниматель", true),
                "ООО" => new("ООО", "Общество с ограниченной ответственностью", false),
                "ПАО" => new("ПАО", "Публичное акционерное общество", false),
                "АО" => new("АО", "Акционерное общество", false),
                _ => new(normalized ?? string.Empty, normalized ?? string.Empty, false),
            };
        }

        public string MakeShortCompanyName(string companyName) => IsIndividualEntrepreneur
            ? Join(ShortName, companyName)
            : string.IsNullOrEmpty(companyName) ? ShortName : $"{ShortName} «{companyName}»";

        public string MakeFullCompanyName(string companyName) => IsIndividualEntrepreneur
            ? Join(FullName, companyName)
            : string.IsNullOrEmpty(companyName) ? FullName : $"{FullName} «{companyName}»";

        private static string Join(string first, string second) => string.Join(' ', new[] { first, second }.Where(x => !string.IsNullOrEmpty(x)));
    }
}
