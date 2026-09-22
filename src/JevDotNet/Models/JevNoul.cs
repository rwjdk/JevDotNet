namespace JevDotNet.ClassLib.Models;

public sealed record JevNoul(double Probability)
{
    public bool Value => IsAtLeast(0.5);

    public bool IsAtLeast(double threshold)
    {
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                "A Noul threshold must be a finite value between 0 and 1.");
        }

        return Probability >= threshold;
    }
}
