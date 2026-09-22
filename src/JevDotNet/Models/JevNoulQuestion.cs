namespace JevDotNet.Models;

/// <summary>
/// Defines a Jev Noul question that returns a probability between false and true.
/// </summary>
public sealed class JevNoulQuestion : JevQuestion
{
    /// <summary>
    /// Initializes a Noul question.
    /// </summary>
    /// <param name="instructions">The question or instruction Jev should apply.</param>
    /// <param name="questionId">The stable ID used to map the answer to result properties.</param>
    /// <param name="criteria">Optional descriptions of the true and false outcomes.</param>
    public JevNoulQuestion(
        string instructions,
        string questionId,
        JevNoulCriteria? criteria = null)
        : base(instructions, questionId)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Gets the optional descriptions of the true and false outcomes.
    /// </summary>
    public JevNoulCriteria? Criteria { get; }
}
