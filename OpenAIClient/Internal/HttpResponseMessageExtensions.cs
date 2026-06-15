namespace FillTheDoc.OpenAIClient.Internal;

internal static class HttpResponseMessageExtensions {
    public static TimeSpan? GetRetryAfter(this HttpResponseMessage response) {
        if (response.Headers.RetryAfter?.Delta is { } delta) {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date) {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        if (response.Headers.TryGetValues("Retry-After", out var values)) {
            var raw = values.FirstOrDefault();

            if (double.TryParse(raw, out var seconds)) {
                return TimeSpan.FromSeconds(seconds);
            }
        }

        return null;
    }

    public static string? GetRequestId(this HttpResponseMessage response) {
        if (response.Headers.TryGetValues("x-request-id", out var values)) {
            return values.FirstOrDefault();
        }

        return null;
    }
}