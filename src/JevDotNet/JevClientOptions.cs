namespace JevDotNet;

/// <summary>
/// Configures a <see cref="JevClient"/>.
/// </summary>
public sealed class JevClientOptions
{
    /// <summary>
    /// Gets the TypeSafe AI API key used for authentication.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Gets the Jev model to request.
    /// </summary>
    public string Model { get; init; } = "jev-latest";

    /// <summary>
    /// Gets the Jev API endpoint.
    /// </summary>
    public Uri Endpoint { get; init; } = new("https://api.typesafe.ai/v1/systemone");

    /// <summary>
    /// Gets the factory used to provide the caller-owned HTTP client used by <see cref="JevClient"/>.
    /// </summary>
    public Func<HttpClient> HttpClientFactory { get; init; } = JevClient.GetSharedHttpClient;
}
