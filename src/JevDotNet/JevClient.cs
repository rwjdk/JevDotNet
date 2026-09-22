using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JevDotNet.Models;

namespace JevDotNet;

/// <summary>
/// Evaluates text with the TypeSafe AI Jev API and maps answers to strongly typed result objects.
/// </summary>
public partial class JevClient : IDisposable
{
    private readonly JevClientOptions _options;
    private readonly HttpClient _httpClient;
    private bool _disposed;

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
            throw new ArgumentException("An API key is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new ArgumentException("A model is required.", nameof(options));
        }

        if (!options.Endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint must be an absolute URI.", nameof(options));
        }

        _options = options;
        _httpClient = options.HttpClientFactory()
            ?? throw new InvalidOperationException("The HTTP client factory returned null.");
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        IReadOnlyList<QuestionDefinition> definitions = BuildQuestionDefinitions<T>();
        JevRequest payload = new(_options.Model, input, BuildQuestions(definitions));
        string requestJson = JsonSerializer.Serialize(payload, JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, _options.Endpoint);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        JevResponse rawResponse = JsonSerializer.Deserialize<JevResponse>(responseJson, JsonOptions)
            ?? throw new JsonException("The API response could not be deserialized.");
        T result = ConvertAnswers<T>(definitions, rawResponse);

        return new JevResponse<T>(
            result,
            rawResponse.Usage.InputTokenCount,
            rawResponse.Usage.OutputTokenCount);
    }

    /// <summary>
    /// Releases the HTTP client created by the configured factory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
