namespace WinFillTheDoc.Domain.Placeholders;

public sealed class SamplePlaceholderCatalog : IPlaceholderCatalog
{
    public IReadOnlyList<PlaceholderDefinition> GetAll() =>
    [
        new("organization_name", "Наименование организации", true),
        new("director_name", "ФИО руководителя", true),
        new("contract_number", "Номер договора", true),
        new("contract_date", "Дата договора", true),
        new("organization_address", "Адрес организации", false),
        new("contact_phone", "Контактный телефон", false),
    ];
}
