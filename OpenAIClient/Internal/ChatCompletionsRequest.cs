using System.Text.Json.Serialization;

namespace FillTheDoc.OpenAIClient.Internal;

internal sealed record ChatCompletionsRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("response_format")] ResponseFormat? ResponseFormat);

internal sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record ResponseFormat(
    [property: JsonPropertyName("type")] string Type);