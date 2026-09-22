namespace JevDotNet.Models;

/// <summary>
/// Defines a Noul question on a result property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JevNoulQuestionAttribute : Attribute
{
    /// <summary>
    /// Initializes a Noul question.
    /// </summary>
    /// <param name="question">The question Jev should answer.</param>
    /// <param name="threshold">The inclusive threshold used when converting the answer to a Boolean value.</param>
    public JevNoulQuestionAttribute(string question, double threshold = 0.5)
    {
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                $"A Noul threshold must be a finite value between 0 and 1, inclusive. " +
                $"Choose a value such as 0.5 or 0.7. See {JevDotNet.WikiLinks.Noul}.");
        }

        Question = question;
        Threshold = threshold;
    }

    /// <summary>
    /// Gets the question Jev should answer.
    /// </summary>
    public string Question { get; }

    /// <summary>
    /// Gets or sets the optional description of the true outcome.
    /// </summary>
    public string? True { get; set; }

    /// <summary>
    /// Gets or sets the optional description of the false outcome.
    /// </summary>
    public string? False { get; set; }

    /// <summary>
    /// Gets the inclusive threshold used when converting the answer to a Boolean value.
    /// </summary>
    public double Threshold { get; }

    internal bool HasCustomThreshold => Math.Abs(Threshold - 0.5) > 0.001;
}
