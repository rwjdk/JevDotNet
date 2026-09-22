namespace JevDotNet.ClassLib.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JevNoulThresholdAttribute : Attribute
{
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

    public double Threshold { get; }
}
