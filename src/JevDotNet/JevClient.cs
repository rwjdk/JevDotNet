using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JevDotNet.ClassLib.Models;

namespace JevDotNet.ClassLib;

public partial class JevClient
{
    private readonly JevClientOptions _options;
    private readonly HttpClient _httpClient;

    public JevClient(string apiKey)
        : this(new JevClientOptions { ApiKey = apiKey })
    {
    }

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

    public async Task<JevResponse<T>> EvaluateAsync<T>(string input, IList<JevQuestion> questions)
    {
        JevRequest payload = new(_options.Model, input, BuildQuestions(questions));
        string requestJson = JsonSerializer.Serialize(payload, JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, _options.Endpoint);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        string responseJson = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        JevResponse rawResponse = JsonSerializer.Deserialize<JevResponse>(responseJson, JsonOptions)
            ?? throw new JsonException("The API response could not be deserialized.");
        T result = ConvertAnswers<T>(questions, rawResponse);

        return new JevResponse<T>(result, rawResponse);
    }
}
