using System.Net;
using System.Net.Http;
using MainSpringTwo.Web.Models.Plugins;
using MainSpringTwo.Web.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace MainSpringTwo.Tests;

public class HttpPluginBaseTests
{
    [Fact]
    public async Task RunAsync_SendsConfiguredRequestAndReturnsSuccessResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("accepted")
        });
        var client = new HttpClient(handler);
        var plugin = new TestHttpPlugin(new TestHttpClientFactory(client));

        var result = await plugin.RunAsync(new Dictionary<string, string>
        {
            ["Mode"] = "sandbox"
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Patch, handler.Method);
        Assert.Equal("https://example.test/api/jobs?mode=sandbox", handler.RequestUri);
        Assert.True(handler.Headers.Contains("X-Test-Plugin"));
        Assert.Equal("{\"mode\":\"sandbox\"}", handler.Body);
        Assert.Single(result.Logs);
        Assert.Contains("202", result.Logs[0].Message);
        Assert.Equal("accepted", result.Logs[0].Detail);
    }

    [Fact]
    public async Task RunAsync_ReturnsFailureResult_WhenRequestThrowsHttpRequestException()
    {
        var handler = new RecordingHandler(_ => throw new HttpRequestException("boom"));
        var client = new HttpClient(handler);
        var plugin = new TestHttpPlugin(new TestHttpClientFactory(client));

        var result = await plugin.RunAsync(new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(result.Logs);
        Assert.True(result.Logs[0].IsError);
        Assert.Contains("request failed", result.Logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListParameter_CanExposeConfiguredOptions()
    {
        var parameter = TestHttpPlugin.Parameters.Single();

        Assert.Equal(ParameterDataType.List, parameter.DataType);
        Assert.Collection(
            parameter.Options,
            option =>
            {
                Assert.Equal("sandbox", option.Value);
                Assert.Equal("Sandbox", option.Label);
            },
            option =>
            {
                Assert.Equal("production", option.Value);
                Assert.Equal("Production", option.Label);
            });
    }

    [Fact]
    public void DerivedPlugin_CanBeResolvedFromDependencyInjection_WhenHttpClientFactoryIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<TestHttpPlugin>();

        using var provider = services.BuildServiceProvider();
        var plugin = provider.GetRequiredService<TestHttpPlugin>();

        Assert.NotNull(plugin);
    }

    private sealed class TestHttpPlugin : HttpPluginBase
    {
        public static List<PluginParameter> Parameters =>
        [
            new()
            {
                Name = "Mode",
                Description = "Execution mode.",
                DataType = ParameterDataType.List,
                Options =
                [
                    new()
                    {
                        Value = "sandbox",
                        Label = "Sandbox"
                    },
                    new()
                    {
                        Value = "production",
                        Label = "Production"
                    }
                ]
            }
        ];

        public TestHttpPlugin(IHttpClientFactory httpClientFactory)
            : base(httpClientFactory)
        {
        }

        public override string Name => "Test HTTP Plugin";

        public override string Description => "Used for unit tests.";

        public override List<PluginParameter> ConfigurationParameters => Parameters;

        protected override HttpMethod Method => HttpMethod.Patch;

        protected override string Url => "https://example.test/api/jobs";

        protected override IEnumerable<KeyValuePair<string, string?>> GetQueryParameters(Dictionary<string, string> configuration)
        {
            yield return new KeyValuePair<string, string?>("mode", configuration.GetValueOrDefault("Mode", "sandbox"));
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetHeaders(Dictionary<string, string> configuration)
        {
            yield return new KeyValuePair<string, string>("X-Test-Plugin", Name);
        }

        protected override HttpContent? CreateContent(Dictionary<string, string> configuration)
        {
            return CreateJsonContent($"{{\"mode\":\"{configuration.GetValueOrDefault("Mode", "sandbox")}\"}}");
        }
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

        public string Body { get; private set; } = string.Empty;

        public IReadOnlyCollection<string> Headers { get; private set; } = [];

        public HttpMethod? Method { get; private set; }

        public string? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri?.ToString();
            Headers = request.Headers.Select(header => header.Key).ToArray();
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _responseFactory(request);
        }
    }
}
