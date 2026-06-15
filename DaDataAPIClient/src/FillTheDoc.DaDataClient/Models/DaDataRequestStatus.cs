namespace FillTheDoc.DaDataClient.Models;

public sealed record DaDataRequestStatus(
    int HttpStatus,
    int Attempts,
    int DurationMs,
    TimeSpan? RetryAfter);