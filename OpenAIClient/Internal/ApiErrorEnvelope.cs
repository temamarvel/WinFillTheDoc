using System.Text.Json.Serialization;

namespace FillTheDoc.OpenAIClient.Internal;

internal sealed record ApiErrorEnvelope(
    [property: JsonPropertyName("error")] ApiError Error);

internal sealed record ApiError(
    [property: JsonPropertyName("message")] string Message);