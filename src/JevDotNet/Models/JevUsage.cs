using System.Text.Json.Serialization;

namespace JevDotNet.ClassLib.Models;

public sealed record JevUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens);