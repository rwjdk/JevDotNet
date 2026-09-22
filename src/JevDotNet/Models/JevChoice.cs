namespace JevDotNet.Models;

/// <summary>
/// Contains a Choice answer and its probability information.
/// </summary>
/// <typeparam name="T">The enum that defines the available choices.</typeparam>
/// <param name="Choice">The selected choice.</param>
/// <param name="Confidence">The confidence assigned to the selected choice.</param>
/// <param name="Probabilities">The probability assigned to each choice.</param>
public sealed record JevChoice<T>(
    T Choice,
    double Confidence,
    IReadOnlyDictionary<T, double> Probabilities)
    where T : struct, Enum;
