namespace FillTheDoc.DaDataClient.Models;

public sealed record DaDataResult<T>(
    T Value,
    DaDataRequestStatus Status);