using System.Text.Json.Serialization;

namespace FillTheDoc.DaDataClient.Models;

public sealed record DaDataAddressData(
    [property: JsonPropertyName("postal_code")]
    string? PostalCode,
    [property: JsonPropertyName("country")]
    string? Country,
    [property: JsonPropertyName("region_with_type")]
    string? RegionWithType,
    [property: JsonPropertyName("city_with_type")]
    string? CityWithType,
    [property: JsonPropertyName("street_with_type")]
    string? StreetWithType,
    [property: JsonPropertyName("house")]
    string? House,
    [property: JsonPropertyName("block")]
    string? Block,
    [property: JsonPropertyName("flat")]
    string? Flat,
    [property: JsonPropertyName("fias_id")]
    string? FiasId,
    [property: JsonPropertyName("kladr_id")]
    string? KladrId
) {
    public bool IsPreciseToHouseLevel => !string.IsNullOrWhiteSpace(House);

    public bool IsPreciseToStreetLevel => !string.IsNullOrWhiteSpace(StreetWithType);

    public string FormattedShort => string.Join(
        ", ",
        new[] {
            RegionWithType,
            CityWithType,
            StreetWithType,
            string.IsNullOrWhiteSpace(House) ? null : $"д. {House}"
        }.Where(static value => !string.IsNullOrWhiteSpace(value)));
}