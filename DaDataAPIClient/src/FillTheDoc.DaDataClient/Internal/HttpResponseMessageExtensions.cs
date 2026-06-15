using System.Globalization;

namespace FillTheDoc.DaDataClient.Internal;

internal static class HttpResponseMessageExtensions {
    public static TimeSpan? GetRetryAfter(this HttpResponseMessage response) {
        ArgumentNullException.ThrowIfNull(response);

        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is not null) {
            return retryAfter.Delta.Value < TimeSpan.Zero ? TimeSpan.Zero : retryAfter.Delta.Value;
        }

        if (retryAfter?.Date is not null) {
            var delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (response.Headers.TryGetValues("Retry-After", out var values)) {
            var rawValue = values.FirstOrDefault();

            if (rawValue is null) {
                return null;
            }

            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)) {
                return TimeSpan.FromSeconds(Math.Max(0d, seconds));
            }
        }

        return null;
    }

    public static string GetSnippet(this string? content, int maxLength = 8_192) {
        if (string.IsNullOrEmpty(content)) {
            return string.Empty;
        }

        return content.Length <= maxLength
            ? content
            : content[..maxLength];
    }
}