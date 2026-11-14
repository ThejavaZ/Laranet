using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Laranet.Tests.TestSupport;

/// <summary>
/// A controllable <see cref="HttpMessageHandler"/> that records requests and returns
/// queued responses.
/// </summary>
internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responders =
        new();

    public List<RecordedRequest> Requests { get; } = [];

    public void EnqueueResponse(HttpResponseMessage response)
    {
        EnqueueResponse(() => response);
    }

    public void EnqueueResponse(Func<HttpResponseMessage> responseFactory)
    {
        Enqueue((_, _) => Task.FromResult(responseFactory()));
    }

    public void EnqueueJson(HttpStatusCode statusCode, string json)
    {
        EnqueueResponse(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }

    public void EnqueueStatus(HttpStatusCode statusCode, string? body = null)
    {
        EnqueueResponse(() => body is null
            ? new HttpResponseMessage(statusCode)
            : new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            });
    }

    public void EnqueueException(Exception exception)
    {
        Enqueue((_, _) => Task.FromException<HttpResponseMessage>(exception));
    }

    public void Enqueue(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responders.Enqueue(responder);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(await RecordedRequest.CreateAsync(request, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();

        if (_responders.Count == 0)
        {
            throw new InvalidOperationException(
                "No response was configured for the request.");
        }

        return await _responders.Dequeue()(request, cancellationToken);
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri? RequestUri,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ContentHeaders,
    string? Content)
{
    public static async Task<RecordedRequest> CreateAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new RecordedRequest(
            request.Method,
            request.RequestUri,
            CaptureHeaders(request.Headers),
            request.Content is null
                ? EmptyHeaders
                : CaptureHeaders(request.Content.Headers),
            content);
    }

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyHeaders =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CaptureHeaders(
        HttpHeaders headers)
    {
        return headers.ToDictionary(
            header => header.Key,
            header => (IReadOnlyList<string>)header.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }
}
