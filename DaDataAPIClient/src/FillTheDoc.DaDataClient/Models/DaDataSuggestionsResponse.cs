using System.Text.Json.Serialization;

namespace FillTheDoc.DaDataClient.Models;

public sealed record DaDataSuggestionsResponse<T>(
    [property: JsonPropertyName("suggestions")]
    IReadOnlyList<DaDataSuggestion<T>> Suggestions);