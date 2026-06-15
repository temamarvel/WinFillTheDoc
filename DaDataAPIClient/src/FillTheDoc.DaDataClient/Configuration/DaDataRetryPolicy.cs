namespace FillTheDoc.DaDataClient.Configuration;

public sealed record DaDataRetryPolicy(
    int MaxAttempts,
    TimeSpan BaseDelay,
    TimeSpan MaxDelay
) {
    public static DaDataRetryPolicy Default { get; } = new(
        MaxAttempts: 4,
        BaseDelay: TimeSpan.FromMilliseconds(400),
        MaxDelay: TimeSpan.FromSeconds(6));
}