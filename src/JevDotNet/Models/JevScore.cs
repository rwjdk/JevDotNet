namespace JevDotNet.ClassLib.Models;

public sealed record JevScore<T>(
    double Score,
    double Confidence,
    IReadOnlyDictionary<T, double> Probabilities,
    IReadOnlyDictionary<T, string> Legend)
    where T : struct, Enum;
