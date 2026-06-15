namespace FillTheDoc.OpenAIClient.Errors;

public sealed class OpenAIDecodingException : OpenAIClientException {
    public OpenAIDecodingException(string message, Exception? innerException = null)
        : base(message, innerException) {
    }
}