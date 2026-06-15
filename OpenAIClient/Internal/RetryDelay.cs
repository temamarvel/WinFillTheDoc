namespace FillTheDoc.OpenAIClient.Internal;

internal static class RetryDelay {
    public static TimeSpan Calculate(int attempt, TimeSpan? retryAfter) {
        if (retryAfter is not null) {
            return retryAfter.Value;
        }

        var baseDelayMs = 500 * Math.Pow(2.0, Math.Max(0, attempt - 1));
        var jitterMs = Random.Shared.NextDouble() * baseDelayMs * 0.25;
        return TimeSpan.FromMilliseconds(baseDelayMs + jitterMs);
    }
}