using System.Text.Json.Serialization;

namespace JevDotNet.Models;

internal sealed record JevUsage(
    [property: JsonPropertyName("input_tokens")] long InputTokenCount,
    [property: JsonPropertyName("output_tokens")] long OutputTokenCount);
