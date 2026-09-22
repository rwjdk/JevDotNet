using System.Text.Json;
using System.Text.Json.Serialization;

namespace JevDotNet.ClassLib.Models;

public sealed record JevResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("answers")] Dictionary<string, JsonElement> Answers,
    [property: JsonPropertyName("usage")] JevUsage Usage);

public sealed record JevResponse<T>(
    T Result,
    JevResponse Raw)
{
    public JevUsage Usage => Raw.Usage;
}
