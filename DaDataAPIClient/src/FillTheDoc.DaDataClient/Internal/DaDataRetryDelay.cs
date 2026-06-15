using FillTheDoc.DaDataClient.Configuration;

namespace FillTheDoc.DaDataClient.Internal;

internal static class DaDataRetryDelay {
    public static TimeSpan Calculate(
        int attempt,
        TimeSpan? retryAfter,
        DaDataRetryPolicy policy) {
        if (retryAfter is not null) {
            return retryAfter.Value <= policy.MaxDelay
                ? retryAfter.Value
                : policy.MaxDelay;
        }

        var exponentialMs = Math.Min(
            policy.MaxDelay.TotalMilliseconds,
            policy.BaseDelay.TotalMilliseconds * Math.Pow(2.0, attempt - 1));

        var jitterMs = Random.Shared.NextDouble() * exponentialMs * 0.25;

        return TimeSpan.FromMilliseconds(
            Math.Min(policy.MaxDelay.TotalMilliseconds, exponentialMs + jitterMs));
    }
}