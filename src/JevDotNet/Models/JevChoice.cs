namespace JevDotNet.ClassLib.Models;

public sealed record JevChoice<T>(
    T Choice,
    double Confidence,
    IReadOnlyDictionary<T, double> Probabilities)
    where T : struct, Enum;
