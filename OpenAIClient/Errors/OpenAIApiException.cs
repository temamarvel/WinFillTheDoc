namespace FillTheDoc.OpenAIClient.Errors;

public sealed class OpenAIApiException : OpenAIClientException {
    public OpenAIApiException(string message)
        : base($"OpenAI API error: {message}") {
        ApiMessage = message;
    }

    public string ApiMessage { get; }
}