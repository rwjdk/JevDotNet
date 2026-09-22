namespace JevDotNet.Models;

/// <summary>
/// Contains a Score answer and its level information.
/// </summary>
/// <typeparam name="T">The enum that defines the ordered score levels.</typeparam>
/// <param name="Score">The numeric score.</param>
/// <param name="Confidence">The confidence assigned to the score.</param>
/// <param name="Probabilities">The probability assigned to each level.</param>
/// <param name="Legend">The description of each level returned by Jev.</param>
public sealed record JevScore<T>(
    double Score,
    double Confidence,
    IReadOnlyDictionary<T, double> Probabilities,
    IReadOnlyDictionary<T, string> Legend)
    where T : struct, Enum;
