namespace WinFillTheDoc.Domain.Placeholders;

public sealed class PlaceholderRegistry : IPlaceholderCatalog
{
    private static readonly ChoiceInputConfiguration PaymentMethodChoices = new(["счет", "сбп"]);

    private readonly IReadOnlyList<PlaceholderDescriptor> _descriptors =
    [
        new("company_name", "Название компании", "Краткое наименование без правовой формы.", PlaceholderSection.Company, 10, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true, "Ромашка"),
        new("legal_form", "Правовая форма", "Аббревиатура правовой формы: ООО, АО, ИП и т.д.", PlaceholderSection.Company, 20, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true, "ООО"),
        new("ceo_full_name", "Руководитель (полное имя)", "Фамилия Имя Отчество в именительном падеже.", PlaceholderSection.Company, 30, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true, "Иванов Иван Иванович"),
        new("ceo_full_genitive_name", "Руководитель (родительный падеж)", "ФИО руководителя в родительном падеже.", PlaceholderSection.Company, 40, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true, "Иванова Ивана Ивановича"),
        new("ceo_shorten_name", "Руководитель (кратко)", "Фамилия с инициалами руководителя.", PlaceholderSection.Company, 50, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true, "Иванов И.И."),
        new("ogrn", "ОГРН / ОГРНИП", "13 цифр для юридического лица, 15 для ИП.", PlaceholderSection.Company, 60, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true),
        new("inn", "ИНН", "10 цифр для юридического лица, 12 для ИП.", PlaceholderSection.Company, 70, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, true),
        new("kpp", "КПП", "9 цифр. Указывается для юридических лиц.", PlaceholderSection.Company, 80, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, false),
        new("email", "Email", "Электронная почта организации.", PlaceholderSection.Company, 90, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, false),
        new("address", "Адрес", "Юридический или фактический адрес.", PlaceholderSection.Company, 100, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, false),
        new("phone", "Телефон", "Контактный телефон.", PlaceholderSection.Company, 110, PlaceholderValueSource.Extracted, PlaceholderInputKind.Text, false),
        new("document_number", "Номер документа", "Номер договора или другого документа.", PlaceholderSection.Document, 120, PlaceholderValueSource.Manual, PlaceholderInputKind.Text, false),
        new("fee", "Комиссия, %", "Размер комиссионного вознаграждения.", PlaceholderSection.Document, 130, PlaceholderValueSource.Manual, PlaceholderInputKind.Text, true),
        new("min_fee", "Мин. комиссия, руб.", "Минимальный размер комиссии.", PlaceholderSection.Document, 140, PlaceholderValueSource.Manual, PlaceholderInputKind.Text, true),
        new("payment_method", "Способ оплаты", "Способ оплаты документа.", PlaceholderSection.Document, 150, PlaceholderValueSource.Manual, PlaceholderInputKind.Choice, true),
        new("date_long", "Дата (полная)", "Вычисляется системой.", PlaceholderSection.Computed, 210, null, PlaceholderInputKind.Text, false),
        new("date_short", "Дата (краткая)", "Вычисляется системой.", PlaceholderSection.Computed, 220, null, PlaceholderInputKind.Text, false),
        new("ceo_role", "Должность руководителя", "Вычисляется системой.", PlaceholderSection.Computed, 230, null, PlaceholderInputKind.Text, false),
        new("full_company_name", "Полное наименование", "Вычисляется системой.", PlaceholderSection.Computed, 240, null, PlaceholderInputKind.Text, false),
        new("full_company_name_expanded", "Полное наименование (развернуто)", "Вычисляется системой.", PlaceholderSection.Computed, 250, null, PlaceholderInputKind.Text, false),
        new("rules", "Основание деятельности", "Вычисляется системой.", PlaceholderSection.Computed, 260, null, PlaceholderInputKind.Text, false),
    ];

    private readonly Dictionary<string, PlaceholderFieldPolicy> _policies;

    public PlaceholderRegistry()
    {
        _policies = new(StringComparer.Ordinal)
        {
            ["company_name"] = new(validate: FieldValidators.NonEmpty),
            ["legal_form"] = new(value => value.Trim().ToUpperInvariant(), FieldValidators.NonEmpty),
            ["ceo_full_name"] = new(validate: FieldValidators.FullName),
            ["ceo_full_genitive_name"] = new(validate: FieldValidators.FullName),
            ["ceo_shorten_name"] = new(validate: FieldValidators.ShortName),
            ["ogrn"] = new(FieldNormalizers.DigitsOnly, FieldValidators.Ogrn),
            ["inn"] = new(FieldNormalizers.DigitsOnly, FieldValidators.Inn),
            ["kpp"] = new(FieldNormalizers.DigitsOnly, FieldValidators.Kpp),
            ["email"] = new(value => value.Trim().ToLowerInvariant(), FieldValidators.Email),
            ["address"] = new(validate: FieldValidators.Address),
            ["phone"] = new(FieldNormalizers.Phone, FieldValidators.Phone),
            ["document_number"] = new(),
            ["fee"] = new(validate: value => FieldValidators.DecimalInRange(value, 0, 100)),
            ["min_fee"] = new(validate: value => FieldValidators.DecimalInRange(value, 10, 1000)),
            ["payment_method"] = new(validate: value => FieldValidators.Choice(value, PaymentMethodChoices)),
        };
    }

    public IReadOnlyList<PlaceholderDescriptor> GetAll() => _descriptors;

    public IReadOnlyList<PlaceholderDescriptor> GetInputDescriptors() => _descriptors
        .Where(x => x.AcceptsUserInput).OrderBy(x => x.Section).ThenBy(x => x.Order).ToList();

    public PlaceholderFieldPolicy GetFieldPolicy(string key) =>
        _policies.TryGetValue(key, out var policy) ? policy : new PlaceholderFieldPolicy();

    public ChoiceInputConfiguration? GetChoiceConfiguration(string key) => key == "payment_method" ? PaymentMethodChoices : null;
}
