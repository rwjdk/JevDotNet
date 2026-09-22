namespace JevDotNet.Models;

/// <summary>
/// Contains a Noul probability.
/// </summary>
/// <param name="Probability">The probability from 0 to 1.</param>
public sealed record JevNoul(double Probability)
{
    /// <summary>
    /// Gets whether the probability is at least the default threshold of 0.5.
    /// </summary>
    public bool Value => IsAtLeast(0.5);

    /// <summary>
    /// Determines whether the probability meets or exceeds a threshold.
    /// </summary>
    /// <param name="threshold">A threshold from 0 to 1, inclusive.</param>
    /// <returns><see langword="true"/> when the probability is at least the threshold.</returns>
    public bool IsAtLeast(double threshold)
    {
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                $"A Noul threshold must be a finite value between 0 and 1, inclusive. " +
                $"Choose a value such as 0.5 or 0.7. See {JevDotNet.WikiLinks.Noul}.");
        }

        return Probability >= threshold;
    }
}
