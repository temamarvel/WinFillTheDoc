namespace FillTheDoc.DaDataClient.Errors;

public sealed class DaDataTimeoutException : DaDataClientException {
    public DaDataTimeoutException(Exception? innerException = null)
        : base("DaData request timed out.", innerException) {
    }
}