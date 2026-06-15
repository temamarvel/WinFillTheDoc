using System.Text.Json.Serialization;

namespace FillTheDoc.DaDataClient.Models;

public sealed record DaDataCompanyInfo(
    [property: JsonPropertyName("inn")] string? Inn,
    [property: JsonPropertyName("kpp")] string? Kpp,
    [property: JsonPropertyName("ogrn")] string? Ogrn,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("name")] DaDataCompanyName? Name,
    [property: JsonPropertyName("management")] DaDataCompanyManagement? Management,
    [property: JsonPropertyName("address")] DaDataCompanyAddress? Address,
    [property: JsonPropertyName("state")] DaDataCompanyState? State,
    [property: JsonPropertyName("okved")] string? Okved,
    [property: JsonPropertyName("okved_type")] string? OkvedType);

public sealed record DaDataCompanyName(
    [property: JsonPropertyName("full_with_opf")] string? FullWithOpf,
    [property: JsonPropertyName("short_with_opf")] string? ShortWithOpf,
    [property: JsonPropertyName("latin")] string? Latin,
    [property: JsonPropertyName("full")] string? Full,
    [property: JsonPropertyName("short")] string? Short);

public sealed record DaDataCompanyManagement(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("post")] string? Post);

public sealed record DaDataCompanyState(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("registration_date")] long? RegistrationDate,
    [property: JsonPropertyName("liquidation_date")] long? LiquidationDate);

public sealed record DaDataCompanyAddress(
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("unrestricted_value")] string? UnrestrictedValue,
    [property: JsonPropertyName("data")] DaDataAddressData? Data);