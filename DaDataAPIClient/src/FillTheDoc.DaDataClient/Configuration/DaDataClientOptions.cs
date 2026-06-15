namespace FillTheDoc.DaDataClient.Configuration;

public sealed class DaDataClientOptions {
    public Uri BaseUrl { get; set; } = new("https://suggestions.dadata.ru/");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    public DaDataRetryPolicy RetryPolicy { get; set; } = DaDataRetryPolicy.Default;

    internal void Validate() {
        if (BaseUrl is null) {
            throw new InvalidOperationException("DaData BaseUrl is not configured.");
        }

        if (!BaseUrl.IsAbsoluteUri) {
            throw new InvalidOperationException("DaData BaseUrl must be an absolute URI.");
        }

        if (Timeout <= TimeSpan.Zero) {
            throw new InvalidOperationException("DaData Timeout must be greater than zero.");
        }

        if (RetryPolicy.MaxAttempts < 1) {
            throw new InvalidOperationException("DaData RetryPolicy.MaxAttempts must be at least 1.");
        }

        if (RetryPolicy.BaseDelay < TimeSpan.Zero) {
            throw new InvalidOperationException("DaData RetryPolicy.BaseDelay cannot be negative.");
        }

        if (RetryPolicy.MaxDelay < TimeSpan.Zero) {
            throw new InvalidOperationException("DaData RetryPolicy.MaxDelay cannot be negative.");
        }
    }
}