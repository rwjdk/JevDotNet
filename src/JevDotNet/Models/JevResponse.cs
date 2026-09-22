using System.Text.Json;
using System.Text.Json.Serialization;

namespace JevDotNet.Models;

internal sealed record JevResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("answers")] Dictionary<string, JsonElement> Answers,
    [property: JsonPropertyName("usage")] JevUsage Usage);

/// <summary>
/// Contains a strongly typed result and its token counts.
/// </summary>
/// <typeparam name="T">The converted result type.</typeparam>
/// <param name="Result">The converted result object.</param>
/// <param name="InputTokenCount">The number of input tokens consumed.</param>
/// <param name="OutputTokenCount">The number of output tokens generated.</param>
public sealed record JevResponse<T>(
    T Result,
    long InputTokenCount,
    long OutputTokenCount);
