using System.Text.Json.Serialization;

namespace JevDotNet.ClassLib.Models;

internal sealed record JevRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("state")] object State,
    [property: JsonPropertyName("questions")] Dictionary<string, object> Questions);