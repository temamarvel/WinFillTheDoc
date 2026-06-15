namespace FillTheDoc.OpenAIClient.Errors;

public sealed class OpenAINetworkException : OpenAIClientException {
    public OpenAINetworkException(string message, Exception? innerException = null)
        : base(message, innerException) {
    }
}