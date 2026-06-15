namespace FillTheDoc.OpenAIClient.Errors;

public abstract class OpenAIClientException : Exception {
    protected OpenAIClientException(string message, Exception? innerException = null)
        : base(message, innerException) {
    }
}