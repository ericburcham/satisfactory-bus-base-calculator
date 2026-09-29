using System.Net;
using System.Text;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that answers every request from a callback (or with fixed content) and
/// records the requests, so tests never touch the network.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
    private readonly List<Uri?> _requests = [];

    /// <summary>Answers every request with the given callback.</summary>
    /// <param name="respond">Builds the response for a request.</param>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        ArgumentNullException.ThrowIfNull(respond);
        _respond = respond;
    }

    /// <summary>Answers every request with <paramref name="content"/> (UTF-8) and <paramref name="status"/>.</summary>
    /// <param name="content">The response body.</param>
    /// <param name="status">The status code.</param>
    public FakeHttpMessageHandler(string content, HttpStatusCode status = HttpStatusCode.OK)
        : this(_ => new HttpResponseMessage(status) { Content = new StringContent(content, Encoding.UTF8) })
    {
    }

    /// <summary>The number of requests received.</summary>
    public int CallCount => _requests.Count;

    /// <summary>The URIs of the requests received, in order.</summary>
    public IReadOnlyList<Uri?> Requests => _requests;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requests.Add(request.RequestUri);
        return Task.FromResult(_respond(request));
    }
}
