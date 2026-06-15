namespace FillTheDoc.OpenAIClient.Errors;

public sealed class OpenAITimeoutException : OpenAIClientException {
    public OpenAITimeoutException()
        : base("Timeout") {
    }
}