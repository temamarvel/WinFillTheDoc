namespace FillTheDoc.DaDataClient.Errors;

public sealed class DaDataNetworkException : DaDataClientException {
    public DaDataNetworkException(string message, Exception innerException)
        : base(message, innerException) {
    }
}