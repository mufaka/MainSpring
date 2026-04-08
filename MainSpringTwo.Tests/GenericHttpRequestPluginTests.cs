using System.Net;
using System.Net.Http;
using MainSpringTwo.Web.Plugins;

namespace MainSpringTwo.Tests;

public class GenericHttpRequestPluginTests
{
    [Fact]
    public async Task RunAsync_UsesConfiguredMethodUrlAndHeaders()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        });
        var client = new HttpClient(handler);
        var plugin = new GenericHttpRequestPlugin(new TestHttpClientFactory(client));

        var result = await plugin.RunAsync(new Dictionary<string, string>
        {
            ["RequestMethod"] = "POST",
            ["RequestUrl"] = "https://example.test/hooks/run",
            ["AuthorizationHeader"] = "Bearer super-secret",
            ["AdditionalHeaders"] = "X-Correlation-Id: 123\nX-Environment: qa"
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://example.test/hooks/run", handler.RequestUri);
        Assert.Equal("Bearer super-secret", handler.Headers["Authorization"]);
        Assert.Equal("123", handler.Headers["X-Correlation-Id"]);
        Assert.Equal("qa", handler.Headers["X-Environment"]);
    }

    [Fact]
    public async Task RunAsync_ReturnsFailure_WhenAdditionalHeaderFormatIsInvalid()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var plugin = new GenericHttpRequestPlugin(new TestHttpClientFactory(client));

        var result = await plugin.RunAsync(new Dictionary<string, string>
        {
            ["RequestMethod"] = "GET",
            ["RequestUrl"] = "https://example.test/hooks/run",
            ["AdditionalHeaders"] = "BrokenHeaderLine"
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(result.Logs);
        Assert.Contains("invalid header entry", result.Logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public TestHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HttpMethod? Method { get; private set; }

        public string? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri?.ToString();

            foreach (var header in request.Headers)
            {
                Headers[header.Key] = string.Join(",", header.Value);
            }

            return Task.FromResult(_responseFactory(request));
        }
    }
}
