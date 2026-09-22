namespace JevDotNet.Models;

/// <summary>
/// Defines a Jev Score question whose ordered levels come from an enum.
/// </summary>
/// <typeparam name="T">The enum that defines between 2 and 10 ordered score levels.</typeparam>
public class JevScoreQuestion<T> : JevQuestion where T : struct, Enum
{
    /// <summary>
    /// Initializes a Score question.
    /// </summary>
    /// <param name="instructions">The question or instruction Jev should apply.</param>
    /// <param name="questionId">The stable ID used to map the answer to result properties.</param>
    public JevScoreQuestion(string instructions, string questionId) : base(instructions, questionId)
    {
    }
}
