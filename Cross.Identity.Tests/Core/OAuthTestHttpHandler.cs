namespace Cross.Identity.Tests.Core;

internal sealed class OAuthTestHttpHandler : HttpMessageHandler
{
    private readonly IReadOnlyDictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes;

    public OAuthTestHttpHandler(IReadOnlyDictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> routes)
    {
        _routes = routes;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        foreach (var (prefix, handler) in _routes.OrderByDescending(static x => x.Key.Length))
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(handler(request));
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No handler for {url}", Encoding.UTF8, "text/plain"),
        });
    }

    public static HttpResponseMessage JsonResponse(HttpStatusCode status, object body)
        => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
}
