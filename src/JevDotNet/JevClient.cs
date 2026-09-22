using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JevDotNet.Models;

namespace JevDotNet;

/// <summary>
/// Evaluates text with the TypeSafe AI Jev API and maps answers to strongly typed result objects.
/// </summary>
public partial class JevClient
{
    private static readonly HttpClient SharedHttpClient = new();
    private readonly JevClientOptions _options;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a client with the default model and endpoint.
    /// </summary>
    /// <param name="apiKey">The TypeSafe AI API key.</param>
    public JevClient(string apiKey)
        : this(new JevClientOptions { ApiKey = apiKey })
    {
    }

    /// <summary>
    /// Initializes a client with custom options.
    /// </summary>
    /// <param name="options">The API and HTTP client configuration.</param>
    public JevClient(JevClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                $"A TypeSafe AI API key is required. Set JevClientOptions.ApiKey or pass it to " +
                $"JevClient(string). See {WikiLinks.Troubleshooting}.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new ArgumentException(
                $"JevClientOptions.Model cannot be empty. Use the default 'jev-latest' or set a " +
                $"model name. See {WikiLinks.Troubleshooting}.", nameof(options));
        }

        if (options.Endpoint is null || !options.Endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException(
                $"JevClientOptions.Endpoint must be an absolute URI, such as " +
                $"https://api.typesafe.ai/v1/systemone. See {WikiLinks.Troubleshooting}.", nameof(options));
        }

        _options = options;
        _httpClient = options.HttpClientFactory?.Invoke()
            ?? throw new InvalidOperationException(
                $"JevClientOptions.HttpClientFactory returned null. Return an HttpClient instance. " +
                $"See {WikiLinks.Troubleshooting}.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Evaluates an input and converts the answers into a new result object.
    /// </summary>
    /// <typeparam name="T">The result type to create. It must have a parameterless constructor.</typeparam>
    /// <param name="input">The text to evaluate.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>The converted result together with its token usage.</returns>
    public async Task<JevResponse<T>> EvaluateAsync<T>(
        string input,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QuestionDefinition> definitions = BuildQuestionDefinitions<T>();
        JevRequest payload = new(_options.Model, input, BuildQuestions(definitions));
        string requestJson = JsonSerializer.Serialize(payload, JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, _options.Endpoint);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            throw new HttpRequestException(
                $"Jev API request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"Check the API key, model, and endpoint. See {WikiLinks.Troubleshooting}.",
                exception,
                response.StatusCode);
        }

        JevResponse rawResponse;
        try
        {
            rawResponse = JsonSerializer.Deserialize<JevResponse>(responseJson, JsonOptions)
                ?? throw new JsonException("The response JSON was null.");
        }
        catch (JsonException exception)
        {
            throw new JsonException(
                $"The Jev API response could not be read. Check that the endpoint returns a Jev " +
                $"response with answers and usage. See {WikiLinks.Troubleshooting}.", exception);
        }

        if (rawResponse.Answers is null || rawResponse.Usage is null)
        {
            throw new JsonException(
                $"The Jev API response is missing answers or usage. Check the endpoint and response " +
                $"shape. See {WikiLinks.Troubleshooting}.");
        }
        T result = ConvertAnswers<T>(definitions, rawResponse);

        return new JevResponse<T>(
            result,
            rawResponse.Usage.InputTokenCount,
            rawResponse.Usage.OutputTokenCount);
    }

    internal static HttpClient GetSharedHttpClient() => SharedHttpClient;
}
