using System.Net.Http;
using MainSpringTwo.Web.Models.Plugins;

namespace MainSpringTwo.Web.Plugins
{
    public class GenericHttpRequestPlugin : HttpPluginBase
    {
        private const string MethodParameterName = "RequestMethod";
        private const string UrlParameterName = "RequestUrl";
        private const string AuthorizationHeaderParameterName = "AuthorizationHeader";
        private const string AdditionalHeadersParameterName = "AdditionalHeaders";

        public GenericHttpRequestPlugin(IHttpClientFactory httpClientFactory)
            : base(httpClientFactory)
        {
        }

        public override string Name => "Generic HTTP Request";

        public override string Description => "Sends a configurable HTTP request to a user-supplied URL with optional authorization and custom headers.";

        public override List<PluginParameter> ConfigurationParameters =>
        [
            new()
            {
                Name = MethodParameterName,
                Description = "HTTP method to use for the outgoing request.",
                DataType = ParameterDataType.List,
                Options =
                [
                    new() { Value = HttpMethod.Get.Method, Label = "GET" },
                    new() { Value = HttpMethod.Post.Method, Label = "POST" },
                    new() { Value = HttpMethod.Put.Method, Label = "PUT" },
                    new() { Value = HttpMethod.Patch.Method, Label = "PATCH" },
                    new() { Value = HttpMethod.Delete.Method, Label = "DELETE" }
                ]
            },
            new()
            {
                Name = UrlParameterName,
                Description = "Absolute URL to request when the plugin runs.",
                DataType = ParameterDataType.String
            },
            new()
            {
                Name = AuthorizationHeaderParameterName,
                Description = "Optional Authorization header value, such as a Bearer token.",
                DataType = ParameterDataType.Password
            },
            new()
            {
                Name = AdditionalHeadersParameterName,
                Description = "Optional additional headers, one per line, using the format Header-Name: value.",
                DataType = ParameterDataType.Text
            }
        ];

        protected override HttpMethod Method => HttpMethod.Get;

        protected override string Url => "https://localhost/";

        protected override HttpMethod GetMethod(Dictionary<string, string> configuration)
        {
            var configuredMethod = GetConfigurationValue(configuration, MethodParameterName);
            if (string.IsNullOrWhiteSpace(configuredMethod))
            {
                return HttpMethod.Get;
            }

            return configuredMethod.Trim().ToUpperInvariant() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "PATCH" => HttpMethod.Patch,
                "DELETE" => HttpMethod.Delete,
                _ => throw new InvalidOperationException($"Plugin '{Name}' has an unsupported HTTP method '{configuredMethod}'.")
            };
        }

        protected override string GetUrl(Dictionary<string, string> configuration)
        {
            var configuredUrl = GetConfigurationValue(configuration, UrlParameterName);
            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                throw new InvalidOperationException($"Plugin '{Name}' requires a value for '{UrlParameterName}'.");
            }

            return configuredUrl.Trim();
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetHeaders(Dictionary<string, string> configuration)
        {
            var authorizationHeader = GetConfigurationValue(configuration, AuthorizationHeaderParameterName)?.Trim();
            var additionalHeaders = ParseAdditionalHeaders(GetConfigurationValue(configuration, AdditionalHeadersParameterName));

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                if (additionalHeaders.Any(header => string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Specify the Authorization header only once. Use the dedicated authorization field or remove the Authorization entry from additional headers.");
                }

                yield return new KeyValuePair<string, string>("Authorization", authorizationHeader);
            }

            foreach (var header in additionalHeaders)
            {
                yield return header;
            }
        }

        private static string? GetConfigurationValue(Dictionary<string, string> configuration, string parameterName)
        {
            return configuration.GetValueOrDefault(parameterName);
        }

        private IEnumerable<KeyValuePair<string, string>> ParseAdditionalHeaders(string? rawHeaders)
        {
            if (string.IsNullOrWhiteSpace(rawHeaders))
            {
                yield break;
            }

            var lines = rawHeaders.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n', StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    throw new InvalidOperationException($"Plugin '{Name}' contains an invalid header entry '{line}'. Use the format Header-Name: value.");
                }

                var headerName = line[..separatorIndex].Trim();
                var headerValue = line[(separatorIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(headerName))
                {
                    throw new InvalidOperationException($"Plugin '{Name}' contains an invalid header entry '{line}'. Header names are required.");
                }

                yield return new KeyValuePair<string, string>(headerName, headerValue);
            }
        }
    }
}
