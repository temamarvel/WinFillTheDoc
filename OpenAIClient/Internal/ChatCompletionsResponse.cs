using System.Text.Json.Serialization;

namespace FillTheDoc.OpenAIClient.Internal;

internal sealed record ChatCompletionsResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<Choice> Choices);

internal sealed record Choice(
    [property: JsonPropertyName("message")] ResponseMessage Message);

internal sealed record ResponseMessage(
    [property: JsonPropertyName("content")] string Content);