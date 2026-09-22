using System.Text.Json.Serialization;

namespace JevDotNet.ClassLib.Models;

public sealed record JevNoulCriteria(
    [property: JsonPropertyName("true")] string True,
    [property: JsonPropertyName("false")] string False);
