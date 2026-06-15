using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace FillTheDoc.DaDataClient.Tests.TestInfrastructure;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    public int SendCount { get; private set; }

    public List<CapturedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        SendCount++;
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri,
            request.Headers.Authorization,
            request.Headers.Accept.Select(static header => header.MediaType ?? string.Empty).ToArray(),
            request.Content?.Headers.ContentType?.MediaType,
            body));

        return await _responder(request, cancellationToken).ConfigureAwait(false);
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

internal sealed record CapturedRequest(
    HttpMethod Method,
    Uri? RequestUri,
    AuthenticationHeaderValue? Authorization,
    IReadOnlyList<string> Accept,
    string? ContentType,
    string Body);

