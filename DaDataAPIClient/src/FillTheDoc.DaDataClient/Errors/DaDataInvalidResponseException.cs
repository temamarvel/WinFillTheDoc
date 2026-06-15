namespace FillTheDoc.DaDataClient.Errors;

public sealed class DaDataInvalidResponseException : DaDataClientException {
    public DaDataInvalidResponseException()
        : base("Invalid HTTP response.") {
    }
}