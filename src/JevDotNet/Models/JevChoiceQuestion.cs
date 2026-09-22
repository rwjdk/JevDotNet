namespace JevDotNet.Models;

/// <summary>
/// Defines a Jev Choice question whose options come from an enum.
/// </summary>
/// <typeparam name="T">The enum that defines the available choices.</typeparam>
public class JevChoiceQuestion<T> : JevQuestion where T : struct, Enum
{
    /// <summary>
    /// Initializes a Choice question.
    /// </summary>
    /// <param name="instructions">The question or instruction Jev should apply.</param>
    /// <param name="questionId">The stable ID used to map the answer to result properties.</param>
    public JevChoiceQuestion(string instructions, string questionId) : base(instructions, questionId)
    {
    }
}
