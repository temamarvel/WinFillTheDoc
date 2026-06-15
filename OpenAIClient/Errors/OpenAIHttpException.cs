namespace FillTheDoc.OpenAIClient.Errors;

public sealed class OpenAIHttpException : OpenAIClientException {
    public OpenAIHttpException(int statusCode, string responseSnippet)
        : base($"HTTP {statusCode}: {responseSnippet}") {
        StatusCode = statusCode;
        ResponseSnippet = responseSnippet;
    }

    public int StatusCode { get; }

    public string ResponseSnippet { get; }
}