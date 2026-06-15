namespace FillTheDoc.DaDataClient.Errors;

public abstract class DaDataClientException : Exception {
    protected DaDataClientException(string message, Exception? innerException = null)
        : base(message, innerException) {
    }
}