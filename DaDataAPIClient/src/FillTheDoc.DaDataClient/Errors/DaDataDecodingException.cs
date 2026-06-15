namespace FillTheDoc.DaDataClient.Errors;

public sealed class DaDataDecodingException : DaDataClientException {
    public DaDataDecodingException(string responseSnippet, Exception innerException)
        : base("Failed to decode DaData response.", innerException) {
        ResponseSnippet = responseSnippet;
    }

    public string ResponseSnippet { get; }
}