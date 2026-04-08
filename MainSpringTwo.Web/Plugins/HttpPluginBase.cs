using System.Net.Http;
using System.Text;
using MainSpringTwo.Web.Models.Plugins;

namespace MainSpringTwo.Web.Plugins
{
    /// <summary>
    /// Provides a reusable base implementation for plugins that invoke an HTTP endpoint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Derived plugin types supply the HTTP method, target URL, and any plugin-specific configuration parameters.
    /// </para>
    /// <para>
    /// Override the request composition members to add query string values, headers, request content, or other request customizations.
    /// </para>
    /// <para>
    /// The base implementation handles request execution, common exception handling, response body capture, and default success/error log generation.
    /// </para>
    /// </remarks>
    public abstract class HttpPluginBase : IPlugin
    {
        private const int MaxDetailLength = 4000;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpPluginBase"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Creates <see cref="HttpClient"/> instances used to send plugin requests.</param>
        protected HttpPluginBase(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets the display name of the plugin.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the human-readable description of the plugin.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the configuration parameters exposed by the plugin.
        /// </summary>
        /// <remarks>
        /// Override this property when the derived plugin requires user-supplied values such as list selections, headers, payload values, or query parameters.
        /// </remarks>
        public virtual List<PluginParameter> ConfigurationParameters => [];

        /// <summary>
        /// Gets the HTTP method used when invoking the configured endpoint.
        /// </summary>
        /// <remarks>
        /// This property supplies the default method. Override <see cref="GetMethod(Dictionary{string, string})"/> when the method is configuration-driven.
        /// </remarks>
        protected abstract HttpMethod Method { get; }

        /// <summary>
        /// Gets the absolute URL targeted by the plugin request.
        /// </summary>
        /// <remarks>
        /// This property supplies the default URL. Override <see cref="GetUrl(Dictionary{string, string})"/> or <see cref="BuildRequestUri(Dictionary{string, string})"/>
        /// when the target URI is configuration-driven.
        /// </remarks>
        protected abstract string Url { get; }

        /// <summary>
        /// Executes the plugin by building and sending an HTTP request derived from the supplied configuration.
        /// </summary>
        /// <param name="configuration">The persisted plugin configuration values for the current job execution.</param>
        /// <param name="cancellationToken">A token used to cancel request creation or execution.</param>
        /// <returns>
        /// A <see cref="PluginResult"/> describing the success state and any logs captured during execution.
        /// </returns>
        /// <remarks>
        /// The base implementation creates the request, applies headers and optional content, sends the request using <see cref="IHttpClientFactory"/>,
        /// and converts common failures into plugin log entries.
        /// </remarks>
        public async Task<PluginResult> RunAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken)
        {
            try
            {
                var method = GetMethod(configuration);
                var requestUri = BuildRequestUri(configuration);
                using var request = new HttpRequestMessage(method, requestUri)
                {
                    Content = CreateContent(configuration)
                };

                foreach (var header in GetHeaders(configuration))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                await ConfigureRequestAsync(request, configuration, cancellationToken);

                var client = _httpClientFactory.CreateClient(Name);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var responseBody = response.Content is null
                    ? string.Empty
                    : await response.Content.ReadAsStringAsync(cancellationToken);

                return await CreateResultAsync(request, response, responseBody, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return CreateFailureResult($"The {Name} request was canceled.");
            }
            catch (TaskCanceledException ex)
            {
                return CreateFailureResult($"The {Name} request timed out or was canceled.", ex.ToString());
            }
            catch (HttpRequestException ex)
            {
                return CreateFailureResult($"The {Name} request failed: {ex.Message}", ex.ToString());
            }
            catch (InvalidOperationException ex)
            {
                return CreateFailureResult(ex.Message, ex.ToString());
            }
        }

        /// <summary>
        /// Gets the HTTP method for the current execution.
        /// </summary>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <returns>The HTTP method to apply to the outgoing request.</returns>
        /// <remarks>
        /// Override this member when the request method depends on user configuration instead of a fixed plugin value.
        /// </remarks>
        protected virtual HttpMethod GetMethod(Dictionary<string, string> configuration)
        {
            return Method;
        }

        /// <summary>
        /// Gets the base URL for the current execution.
        /// </summary>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <returns>The URL that will be validated and used when building the request URI.</returns>
        /// <remarks>
        /// Override this member when the request URL depends on user configuration instead of a fixed plugin value.
        /// </remarks>
        protected virtual string GetUrl(Dictionary<string, string> configuration)
        {
            return Url;
        }

        /// <summary>
        /// Builds the final request URI, including any query string values returned by <see cref="GetQueryParameters(Dictionary{string, string})"/>.
        /// </summary>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <returns>The absolute request URI that will be used for the HTTP call.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="Url"/> is not a valid absolute URI.</exception>
        protected virtual Uri BuildRequestUri(Dictionary<string, string> configuration)
        {
            var url = GetUrl(configuration);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException($"Plugin '{Name}' has an invalid URL: '{url}'.");
            }

            var queryParameters = GetQueryParameters(configuration)
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key) && parameter.Value is not null)
                .ToList();

            if (queryParameters.Count == 0)
            {
                return baseUri;
            }

            var builder = new UriBuilder(baseUri);
            var existingQuery = builder.Query.TrimStart('?');
            var appendedQuery = string.Join("&", queryParameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!)}"));

            builder.Query = string.IsNullOrWhiteSpace(existingQuery)
                ? appendedQuery
                : $"{existingQuery}&{appendedQuery}";

            return builder.Uri;
        }

        /// <summary>
        /// Returns query string parameters to append to the request URI.
        /// </summary>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <returns>A sequence of parameter names and values to include in the request query string.</returns>
        /// <remarks>Return <see langword="null"/> values for items that should be excluded from the final query string.</remarks>
        protected virtual IEnumerable<KeyValuePair<string, string?>> GetQueryParameters(Dictionary<string, string> configuration)
        {
            return [];
        }

        /// <summary>
        /// Returns HTTP headers to add to the outgoing request.
        /// </summary>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <returns>A sequence of header names and values to add to the request.</returns>
        protected virtual IEnumerable<KeyValuePair<string, string>> GetHeaders(Dictionary<string, string> configuration)
        {
            return [];
        }

        /// <summary>
        /// Creates the HTTP request body for the outgoing request.
        /// </summary>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <returns>The request content to send, or <see langword="null"/> when the request has no body.</returns>
        /// <remarks>
        /// Override this member for methods such as POST, PUT, or PATCH when a payload is required.
        /// </remarks>
        protected virtual HttpContent? CreateContent(Dictionary<string, string> configuration)
        {
            return null;
        }

        /// <summary>
        /// Performs final request customization before the request is sent.
        /// </summary>
        /// <param name="request">The request message created by the base implementation.</param>
        /// <param name="configuration">The plugin configuration values for the current execution.</param>
        /// <param name="cancellationToken">A token used to cancel asynchronous customization work.</param>
        /// <returns>A task that completes when request customization is finished.</returns>
        /// <remarks>
        /// Override this member when request preparation requires logic beyond simple headers, query parameters, or body generation.
        /// </remarks>
        protected virtual Task ConfigureRequestAsync(
            HttpRequestMessage request,
            Dictionary<string, string> configuration,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates the plugin result from the completed HTTP response.
        /// </summary>
        /// <param name="request">The request that was sent.</param>
        /// <param name="response">The HTTP response returned by the server.</param>
        /// <param name="responseBody">The response body captured as a string.</param>
        /// <param name="cancellationToken">A token used to cancel asynchronous result processing.</param>
        /// <returns>A <see cref="PluginResult"/> representing the outcome of the HTTP call.</returns>
        /// <remarks>
        /// The default implementation reports non-success status codes as errors and stores the response body in the log detail when present.
        /// Override this member when a plugin needs custom success rules or richer logging.
        /// </remarks>
        protected virtual Task<PluginResult> CreateResultAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            string responseBody,
            CancellationToken cancellationToken)
        {
            var message = $"{request.Method} {request.RequestUri} returned {(int)response.StatusCode} {response.ReasonPhrase}.";

            return Task.FromResult(new PluginResult
            {
                Success = response.IsSuccessStatusCode,
                Logs =
                [
                    new PluginLogEntry
                    {
                        IsError = !response.IsSuccessStatusCode,
                        Message = message,
                        Detail = string.IsNullOrWhiteSpace(responseBody) ? null : Truncate(responseBody)
                    }
                ]
            });
        }

        /// <summary>
        /// Creates JSON request content using UTF-8 encoding and the <c>application/json</c> media type.
        /// </summary>
        /// <param name="json">The JSON payload to send.</param>
        /// <returns>A <see cref="StringContent"/> instance suitable for JSON-based requests.</returns>
        protected static StringContent CreateJsonContent(string json)
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Creates a standardized failure result for request setup or execution failures.
        /// </summary>
        /// <param name="message">The primary log message describing the failure.</param>
        /// <param name="detail">Optional detailed diagnostic information.</param>
        /// <returns>A failed <see cref="PluginResult"/> containing a single error log entry.</returns>
        protected static PluginResult CreateFailureResult(string message, string? detail = null)
        {
            return new PluginResult
            {
                Success = false,
                Logs =
                [
                    new PluginLogEntry
                    {
                        IsError = true,
                        Message = message,
                        Detail = string.IsNullOrWhiteSpace(detail) ? null : Truncate(detail)
                    }
                ]
            };
        }

        /// <summary>
        /// Truncates stored detail text to the maximum supported log size.
        /// </summary>
        /// <param name="value">The detail text to trim when necessary.</param>
        /// <returns>The original text when within the limit; otherwise a truncated copy.</returns>
        private static string Truncate(string value)
        {
            return value.Length <= MaxDetailLength
                ? value
                : value[..MaxDetailLength];
        }
    }
}
