namespace FillTheDoc.OpenAIClient.Configuration;

public sealed class OpenAIClientOptions {
    public Uri BaseUrl { get; set; } = new("https://api.openai.com/v1/");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxRetries { get; set; } = 2;

    public string? UserAgent { get; set; } = "FillTheDoc.OpenAIClient/1.0";

    public string Model { get; set; } = "gpt-4.1-mini";
}