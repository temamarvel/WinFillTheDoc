using System.Net;
using OpenAiClientLibrary = FillTheDoc.OpenAIClient.OpenAIClient;
using StaticApiKeyProvider = FillTheDoc.OpenAIClient.StaticOpenAIApiKeyProvider;
using FillTheDoc.OpenAIClient.Configuration;
using FillTheDoc.OpenAIClient.Errors;
using NUnit.Framework;

namespace OpenAIClient.Tests;

public sealed class OpenAiClientTests {
    [Test]
    public async Task RequestAsync_ReturnsText_WhenOpenAIReturnsSuccess() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"hello"}}]}
                                                                      """, "req-1"));

        var client = CreateClient(handler);

        var result = await client.RequestAsync("system", "user");

        Assert.That(result.Value, Is.EqualTo("hello"));
        Assert.That(result.Status.HttpStatus, Is.EqualTo(200));
        Assert.That(result.Status.RequestId, Is.EqualTo("req-1"));
        Assert.That(result.Status.Retries, Is.EqualTo(0));
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        Assert.That(handler.Requests[0].Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(handler.Requests[0].RequestUri!.ToString(), Is.EqualTo("https://api.openai.com/v1/chat/completions"));
    }

    [Test]
    public async Task RequestJsonAsync_DeserializesResponseContent() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"{\"name\":\"Alice\",\"age\":30}"}}]}
                                                                      """));

        var client = CreateClient(handler);

        var result = await client.RequestJsonAsync<PersonDto>("system", "user");

        Assert.That(result.Value.Name, Is.EqualTo("Alice"));
        Assert.That(result.Value.Age, Is.EqualTo(30));

        var requestBody = await handler.Requests[0].Content!.ReadAsStringAsync();
        Assert.That(requestBody, Does.Contain("\"response_format\":{\"type\":\"json_object\"}"));
    }

    [Test]
    public async Task RequestJsonAsync_ThrowsDecodingException_WhenInvalidJson() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"not-json"}}]}
                                                                      """));

        var client = CreateClient(handler);

        Assert.ThrowsAsync<OpenAIDecodingException>(async () => await client.RequestJsonAsync<PersonDto>("system", "user"));
    }

    [Test]
    public async Task RequestAsync_ThrowsApiException_WhenErrorEnvelopeReturned() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.BadRequest, """
                                                                              {"error":{"message":"bad prompt"}}
                                                                              """));

        var client = CreateClient(handler, maxRetries: 0);

        var exception = Assert.ThrowsAsync<OpenAIApiException>(async () => await client.RequestAsync("system", "user"));

        Assert.That(exception!.ApiMessage, Is.EqualTo("bad prompt"));
    }

    [Test]
    public async Task RequestAsync_Retries_On429() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.TooManyRequests, """
                                                                                   {"error":{"message":"rate limited"}}
                                                                                   """));

        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"hello after retry"}}]}
                                                                      """));

        var client = CreateClient(handler, maxRetries: 2);

        var result = await client.RequestAsync("system", "user");

        Assert.That(result.Value, Is.EqualTo("hello after retry"));
        Assert.That(result.Status.Retries, Is.EqualTo(1));
        Assert.That(handler.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task RequestAsync_Retries_On5xx() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.InternalServerError, "server exploded"));
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"recovered"}}]}
                                                                      """));

        var client = CreateClient(handler, maxRetries: 2);

        var result = await client.RequestAsync("system", "user");

        Assert.That(result.Value, Is.EqualTo("recovered"));
        Assert.That(result.Status.Retries, Is.EqualTo(1));
    }

    [Test]
    public async Task RequestAsync_UsesRetryAfterHeader() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.TooManyRequests, """
                                                                                   {"error":{"message":"slow down"}}
                                                                                   """, retryAfterSeconds: 0));

        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"done"}}]}
                                                                      """));

        var client = CreateClient(handler, maxRetries: 2);

        var result = await client.RequestAsync("system", "user");

        Assert.That(result.Value, Is.EqualTo("done"));
        Assert.That(handler.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task RequestAsync_ThrowsHttpException_ForNonRetryableHttpError() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.Forbidden, "forbidden body"));

        var client = CreateClient(handler, maxRetries: 2);

        var exception = Assert.ThrowsAsync<OpenAIHttpException>(async () => await client.RequestAsync("system", "user"));

        Assert.That(exception!.StatusCode, Is.EqualTo(403));
        Assert.That(exception.ResponseSnippet, Does.Contain("forbidden body"));
    }

    [Test]
    public async Task RequestAsync_ThrowsTimeoutException_OnTimeout() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse((_, _) => throw new TaskCanceledException("Simulated timeout."));

        var client = CreateClient(handler, maxRetries: 0);

        Assert.ThrowsAsync<OpenAITimeoutException>(async () => await client.RequestAsync("system", "user"));
    }

    [Test]
    public async Task RequestAsync_PropagatesCancellation() {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(HttpStatusCode.OK, """
                                                                      {"choices":[{"message":{"content":"never"}}]}
                                                                      """));

        var client = CreateClient(handler, maxRetries: 0);
        var cancellationToken = new CancellationToken(canceled: true);

        Assert.ThrowsAsync<OperationCanceledException>(async () => await client.RequestAsync("system", "user", cancellationToken: cancellationToken));
    }

    private static OpenAiClientLibrary CreateClient(FakeHttpMessageHandler handler, int maxRetries = 0) {
        var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        return new OpenAiClientLibrary(
            httpClient,
            new StaticApiKeyProvider("test-key"),
            new OpenAIClientOptions {
                BaseUrl = new Uri("https://api.openai.com/v1/"),
                Timeout = TimeSpan.FromSeconds(10),
                MaxRetries = maxRetries,
                UserAgent = "Tests/1.0",
                Model = "gpt-4.1-mini"
            });
    }

    private static HttpResponseMessage CreateJsonResponse(
        HttpStatusCode statusCode,
        string body,
        string? requestId = null,
        int? retryAfterSeconds = null) {
        var response = new HttpResponseMessage(statusCode) {
            Content = new StringContent(body)
        };

        if (requestId is not null) {
            response.Headers.Add("x-request-id", requestId);
        }

        if (retryAfterSeconds is not null) {
            response.Headers.Add("Retry-After", retryAfterSeconds.Value.ToString());
        }

        return response;
    }

    public sealed class PersonDto {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}