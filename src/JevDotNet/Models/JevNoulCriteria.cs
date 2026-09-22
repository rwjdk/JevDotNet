using System.Text.Json.Serialization;

namespace JevDotNet.Models;

/// <summary>
/// Describes the true and false outcomes of a Noul question.
/// </summary>
/// <param name="True">The description of the true outcome.</param>
/// <param name="False">The description of the false outcome.</param>
public sealed record JevNoulCriteria(
    [property: JsonPropertyName("true")] string True,
    [property: JsonPropertyName("false")] string False);
