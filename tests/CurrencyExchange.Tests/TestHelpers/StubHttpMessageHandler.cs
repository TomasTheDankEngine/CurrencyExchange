using System.Net;

namespace CurrencyExchange.Tests.TestHelpers;

/// <summary>
/// Returns a canned response and records the request. HttpMessageHandler.SendAsync is protected,
/// so a small hand-written stub is simpler here than a mocking library.
/// </summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public int RequestCount { get; private set; }

    public static StubHttpMessageHandler Returning(HttpStatusCode statusCode, string body = "") =>
        new((_, _) => Task.FromResult(Response(statusCode, body)));

    public static HttpResponseMessage Response(HttpStatusCode statusCode, string body = "") =>
        new(statusCode) { Content = new StringContent(body) };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        RequestCount++;
        return respond(request, cancellationToken);
    }
}
