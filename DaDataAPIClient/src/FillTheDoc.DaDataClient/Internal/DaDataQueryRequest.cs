using System.Text.Json.Serialization;

namespace FillTheDoc.DaDataClient.Internal;

internal sealed record DaDataQueryRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("count")] int? Count);