using System.Text.Json.Serialization;

namespace FillTheDoc.DaDataClient.Models;

public sealed record DaDataSuggestion<T>(
    [property: JsonPropertyName("value")]
    string Value,

    [property: JsonPropertyName("unrestricted_value")]
    string UnrestrictedValue,

    [property: JsonPropertyName("data")]
    T Data);

