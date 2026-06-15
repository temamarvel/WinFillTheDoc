using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FillTheDoc.OpenAIClient.Abstractions;
using FillTheDoc.OpenAIClient.Configuration;
using FillTheDoc.OpenAIClient.Errors;
using FillTheDoc.OpenAIClient.Internal;
using FillTheDoc.OpenAIClient.Models;

namespace FillTheDoc.OpenAIClient;

public sealed class OpenAIClient : IOpenAIClient {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient httpClient;
    private readonly IOpenAIApiKeyProvider apiKeyProvider;
    private readonly OpenAIClientOptions options;

    public OpenAIClient(
        HttpClient httpClient,
        IOpenAIApiKeyProvider apiKeyProvider,
        OpenAIClientOptions options) {
        this.httpClient = httpClient;
        this.apiKeyProvider = apiKeyProvider;
        this.options = options;

        if (this.httpClient.BaseAddress is null) {
            this.httpClient.BaseAddress = options.BaseUrl;
        }

        this.httpClient.Timeout = options.Timeout;
    }

    public Task<OpenAIResult<string>> RequestAsync(
        string system,
        string user,
        double temperature = 0.0,
        CancellationToken cancellationToken = default) =>
        RequestCoreAsync(system, user, temperature, jsonMode: false, cancellationToken);

    public async Task<OpenAIResult<T>> RequestJsonAsync<T>(
        string system,
        string user,
        double temperature = 0.0,
        CancellationToken cancellationToken = default) {
        var raw = await RequestCoreAsync(system, user, temperature, jsonMode: true, cancellationToken)
            .ConfigureAwait(false);

        try {
            var value = JsonSerializer.Deserialize<T>(raw.Value, JsonOptions);

            if (value is null) {
                throw new OpenAIDecodingException($"Failed to decode {typeof(T).Name}: response is null.");
            }

            return new OpenAIResult<T>(value, raw.Status);
        }
        catch (JsonException ex) {
            throw new OpenAIDecodingException(
                $"Failed to decode {typeof(T).Name}. Body: {raw.Value}",
                ex);
        }
    }

    private async Task<OpenAIResult<string>> RequestCoreAsync(
        string system,
        string user,
        double temperature,
        bool jsonMode,
        CancellationToken cancellationToken) {
        var start = Stopwatch.StartNew();
        var payload = new ChatCompletionsRequest(
            options.Model,
            [new ChatMessage("system", system), new ChatMessage("user", user)],
            temperature,
            jsonMode ? new ResponseFormat("json_object") : null);

        string requestJson;

        try {
            requestJson = JsonSerializer.Serialize(payload, JsonOptions);
        }
        catch (Exception ex) {
            throw new OpenAIDecodingException($"Failed to encode request: {ex.Message}", ex);
        }

        var apiKey = await apiKeyProvider.GetApiKeyAsync(cancellationToken).ConfigureAwait(false);
        var attempt = 0;

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            using var request = BuildRequest(requestJson, apiKey);

            try {
                using var response = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var requestId = response.GetRequestId();
                var durationMs = (int)start.ElapsedMilliseconds;

                if (response.IsSuccessStatusCode) {
                    ChatCompletionsResponse decoded;

                    try {
                        decoded = JsonSerializer.Deserialize<ChatCompletionsResponse>(body, JsonOptions) ?? throw new OpenAIDecodingException("Bad response JSON: response is null.");
                    }
                    catch (JsonException ex) {
                        throw new OpenAIDecodingException(
                            $"Bad response JSON: {ex.Message}. Body: {body.ToSnippet()}",
                            ex);
                    }

                    var text = decoded.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
                    return new OpenAIResult<string>(
                        text,
                        new RequestStatus((int)response.StatusCode, requestId, attempt - 1, durationMs));
                }

                ApiErrorEnvelope? apiError = null;

                try {
                    apiError = JsonSerializer.Deserialize<ApiErrorEnvelope>(body, JsonOptions);
                }
                catch (JsonException) {
                }

                if (ShouldRetry((int)response.StatusCode, attempt)) {
                    await DelayAsync(attempt, response.GetRetryAfter(), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (apiError?.Error.Message is { Length: > 0 } message) {
                    throw new OpenAIApiException(message);
                }

                throw new OpenAIHttpException((int)response.StatusCode, body.ToSnippet());
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                if (attempt <= options.MaxRetries) {
                    await DelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new OpenAITimeoutException();
            }
            catch (HttpRequestException ex) {
                if (attempt <= options.MaxRetries) {
                    await DelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new OpenAINetworkException(ex.Message, ex);
            }
            catch (Exception ex) when (ex is not OpenAIClientException and not OperationCanceledException) {
                if (attempt <= options.MaxRetries) {
                    await DelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new OpenAINetworkException(ex.Message, ex);
            }
        }
    }

    private HttpRequestMessage BuildRequest(string requestJson, string apiKey) {
        var endpoint = new Uri(options.BaseUrl, "chat/completions");
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (!string.IsNullOrWhiteSpace(options.UserAgent)) {
            request.Headers.UserAgent.ParseAdd(options.UserAgent);
        }

        return request;
    }

    private bool ShouldRetry(int statusCode, int attempt) =>
        attempt <= options.MaxRetries &&
        (statusCode == (int)HttpStatusCode.TooManyRequests || statusCode is >= 500 and <= 599);

    private static Task DelayAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken) =>
        Task.Delay(RetryDelay.Calculate(attempt, retryAfter), cancellationToken);
}




