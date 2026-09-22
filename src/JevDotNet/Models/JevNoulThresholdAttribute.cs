namespace JevDotNet.Models;

/// <summary>
/// Sets the inclusive probability threshold used to convert a Noul answer to a Boolean property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JevNoulThresholdAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with an inclusive threshold.
    /// </summary>
    /// <param name="threshold">A threshold from 0 to 1, inclusive.</param>
    public JevNoulThresholdAttribute(double threshold)
    {
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                "A Noul threshold must be a finite value between 0 and 1.");
        }

        Threshold = threshold;
    }

    /// <summary>
    /// Gets the inclusive probability threshold.
    /// </summary>
    public double Threshold { get; }
}
