using System.Collections.Concurrent;

namespace OpenAIClient.Tests;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler {
    private readonly ConcurrentQueue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public void EnqueueResponse(HttpResponseMessage response) {
        responses.Enqueue((_, _) => Task.FromResult(response));
    }

    public void EnqueueResponse(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory) {
        responses.Enqueue(responseFactory);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        Requests.Add(await CloneRequestAsync(request, cancellationToken).ConfigureAwait(false));

        if (!responses.TryDequeue(out var responseFactory)) {
            throw new InvalidOperationException("No fake response configured.");
        }

        return await responseFactory(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers) {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null) {
            var content = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            clone.Content = new StringContent(content);

            foreach (var header in request.Content.Headers) {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}