namespace JevDotNet.ClassLib;

public sealed class JevClientOptions
{
    public required string ApiKey { get; init; }
    public string Model { get; init; } = "jev-latest";
    public Uri Endpoint { get; init; } = new("https://api.typesafe.ai/v1/systemone");
    public Func<HttpClient> HttpClientFactory { get; init; } = static () => new HttpClient();
}
