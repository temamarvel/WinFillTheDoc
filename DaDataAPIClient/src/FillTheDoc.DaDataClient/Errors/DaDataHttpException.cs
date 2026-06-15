namespace FillTheDoc.DaDataClient.Errors;

public sealed class DaDataHttpException : DaDataClientException {
    public DaDataHttpException(int statusCode, string responseSnippet, TimeSpan? retryAfter)
        : base(retryAfter is null
            ? $"HTTP {statusCode}."
            : $"HTTP {statusCode}. Retry after {retryAfter.Value.TotalSeconds:0.#}s.") {
        StatusCode = statusCode;
        ResponseSnippet = responseSnippet;
        RetryAfter = retryAfter;
    }

    public int StatusCode { get; }

    public string ResponseSnippet { get; }

    public TimeSpan? RetryAfter { get; }
}