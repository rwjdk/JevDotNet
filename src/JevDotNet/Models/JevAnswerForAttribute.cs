namespace JevDotNet.Models;

/// <summary>
/// Maps a result property to a question ID when convention-based mapping is insufficient.
/// </summary>
/// <param name="questionId">The ID of the question whose answer populates the property.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JevAnswerForAttribute(string questionId) : Attribute
{
    /// <summary>
    /// Gets the mapped question ID.
    /// </summary>
    public string QuestionId { get; } = questionId;
}
