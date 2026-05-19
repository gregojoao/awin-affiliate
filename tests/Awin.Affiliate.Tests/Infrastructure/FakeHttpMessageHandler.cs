using System.Net;

namespace Awin.Affiliate.Tests.Infrastructure;

/// <summary>
/// Lightweight HttpMessageHandler used to stub Awin HTTP traffic in tests. Captures every
/// request and returns whatever the registered responder produces. Supports a queue of
/// responses for retry scenarios.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();

    private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;
    private int _callCount;

    public FakeHttpMessageHandler(HttpResponseMessage response)
        : this((_, _) => response)
    {
    }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    public static FakeHttpMessageHandler Json(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        return new FakeHttpMessageHandler(response);
    }

    public static FakeHttpMessageHandler Sequence(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        return new FakeHttpMessageHandler((_, _) =>
            queue.Count > 0
                ? queue.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var response = _responder(request, _callCount++);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
