namespace JevDotNet.Models;

/// <summary>
/// Provides the shared identity and instructions for a Jev question.
/// </summary>
/// <param name="instructions">The question or instruction Jev should apply.</param>
/// <param name="questionId">The stable ID used to map the answer to result properties.</param>
public abstract class JevQuestion(string instructions, string questionId)
{
    /// <summary>
    /// Gets the stable ID used to map the answer to result properties.
    /// </summary>
    public string Id { get; } = questionId;

    /// <summary>
    /// Gets the question or instruction Jev should apply.
    /// </summary>
    public string Instructions { get; } = instructions;
}
