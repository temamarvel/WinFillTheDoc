using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FillTheDoc.DaDataClient.Abstractions;
using FillTheDoc.DaDataClient.Configuration;
using FillTheDoc.DaDataClient.Errors;
using FillTheDoc.DaDataClient.Internal;
using FillTheDoc.DaDataClient.Models;

namespace FillTheDoc.DaDataClient;

public sealed class DaDataClient : IDaDataClient {
    private static readonly JsonSerializerOptions DefaultJsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IDaDataTokenProvider _tokenProvider;
    private readonly DaDataClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public DaDataClient(
        HttpClient httpClient,
        IDaDataTokenProvider tokenProvider,
        DaDataClientOptions options) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _jsonOptions = DefaultJsonOptions;
    }

    public async Task<DaDataResult<IReadOnlyList<DaDataSuggestion<DaDataCompanyInfo>>>> FetchCompanyInfoAsync(
        string innOrOgrn,
        int count = 1,
        CancellationToken cancellationToken = default) {
        ValidateQuery(innOrOgrn, nameof(innOrOgrn));
        ValidateCount(count);

        var response = await SendWithRetryAsync<DaDataSuggestionsResponse<DaDataCompanyInfo>>(
                DaDataEndpoints.FindPartyById,
                new DaDataQueryRequest(innOrOgrn, count),
                cancellationToken)
            .ConfigureAwait(false);

        return new DaDataResult<IReadOnlyList<DaDataSuggestion<DaDataCompanyInfo>>>(
            response.Value.Suggestions,
            response.Status);
    }

    public async Task<DaDataResult<DaDataSuggestion<DaDataCompanyInfo>?>> FetchCompanyInfoFirstAsync(
        string innOrOgrn,
        CancellationToken cancellationToken = default) {
        var result = await FetchCompanyInfoAsync(innOrOgrn, 1, cancellationToken).ConfigureAwait(false);
        return new DaDataResult<DaDataSuggestion<DaDataCompanyInfo>?>(
            result.Value.FirstOrDefault(),
            result.Status);
    }

    public async Task<DaDataResult<IReadOnlyList<DaDataSuggestion<DaDataAddress>>>> SuggestAddressAsync(
        string query,
        int count = 1,
        CancellationToken cancellationToken = default) {
        ValidateQuery(query, nameof(query));
        ValidateCount(count);

        var response = await SendWithRetryAsync<DaDataSuggestionsResponse<DaDataAddress>>(
                DaDataEndpoints.SuggestAddress,
                new DaDataQueryRequest(query, count),
                cancellationToken)
            .ConfigureAwait(false);

        return new DaDataResult<IReadOnlyList<DaDataSuggestion<DaDataAddress>>>(
            response.Value.Suggestions,
            response.Status);
    }

    public async Task<DaDataResult<DaDataSuggestion<DaDataAddress>?>> SuggestAddressFirstAsync(
        string query,
        CancellationToken cancellationToken = default) {
        var result = await SuggestAddressAsync(query, 1, cancellationToken).ConfigureAwait(false);
        return new DaDataResult<DaDataSuggestion<DaDataAddress>?>(
            result.Value.FirstOrDefault(),
            result.Status);
    }

    private async Task<DaDataResult<TResponse>> SendWithRetryAsync<TResponse>(
        string endpoint,
        DaDataQueryRequest payload,
        CancellationToken cancellationToken) {
        var attempts = Math.Max(1, _options.RetryPolicy.MaxAttempts);
        var stopwatch = Stopwatch.StartNew();
        Exception? lastError = null;

        for (var attempt = 1; attempt <= attempts; attempt++) {
            try {
                using var request = await CreateRequestAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_options.Timeout);

                using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        timeoutCts.Token)
                    .ConfigureAwait(false);

                var retryAfter = response.GetRetryAfter();
                var responseContent = response.Content is null
                    ? string.Empty
                    : await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) {
                    if (IsRetryable(response.StatusCode) && attempt < attempts) {
                        var delay = DaDataRetryDelay.Calculate(attempt, retryAfter, _options.RetryPolicy);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw new DaDataHttpException(
                        (int)response.StatusCode,
                        responseContent.GetSnippet(),
                        retryAfter);
                }

                TResponse? result;

                try {
                    result = JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
                }
                catch (JsonException exception) {
                    throw new DaDataDecodingException(responseContent.GetSnippet(), exception);
                }

                if (result is null) {
                    throw new DaDataInvalidResponseException();
                }

                return new DaDataResult<TResponse>(
                    result,
                    new DaDataRequestStatus(
                        HttpStatus: (int)response.StatusCode,
                        Attempts: attempt,
                        DurationMs: (int)stopwatch.ElapsedMilliseconds,
                        RetryAfter: retryAfter));
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested) {
                lastError = exception;

                if (attempt < attempts) {
                    var delay = DaDataRetryDelay.Calculate(attempt, retryAfter: null, _options.RetryPolicy);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new DaDataTimeoutException(exception);
            }
            catch (Exception exception) when (IsRetryableTransport(exception) && attempt < attempts) {
                lastError = exception;
                var delay = DaDataRetryDelay.Calculate(attempt, retryAfter: null, _options.RetryPolicy);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (IsTransportFailure(exception)) {
                lastError = exception;
                throw new DaDataNetworkException($"DaData transport error: {exception.Message}", exception);
            }
        }

        if (lastError is DaDataClientException daDataException) {
            throw daDataException;
        }

        throw new DaDataNetworkException("DaData request failed.", lastError ?? new InvalidOperationException("Unknown DaData failure."));
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        string endpoint,
        DaDataQueryRequest payload,
        CancellationToken cancellationToken) {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token)) {
            throw new InvalidOperationException("DaData token provider returned an empty token.");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUrl, endpoint));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);
        request.Content = JsonContent.Create(payload, options: _jsonOptions);

        return request;
    }

    private static bool IsRetryable(HttpStatusCode statusCode) {
        var numeric = (int)statusCode;
        return numeric == 429 || (numeric >= 500 && numeric <= 599);
    }

    private static bool IsRetryableTransport(Exception exception) {
        if (exception is TimeoutException or HttpRequestException) {
            return true;
        }

        return exception.InnerException is TimeoutException or HttpRequestException;
    }

    private static bool IsTransportFailure(Exception exception) {
        return exception is HttpRequestException or TimeoutException;
    }

    private static void ValidateQuery(string value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }

    private static void ValidateCount(int count) {
        if (count < 1) {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be greater than zero.");
        }
    }
}