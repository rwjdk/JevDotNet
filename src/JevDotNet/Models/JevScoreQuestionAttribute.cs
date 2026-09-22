namespace JevDotNet.Models;

/// <summary>
/// Defines a Score question on a result property.
/// </summary>
/// <typeparam name="T">The enum that defines between 2 and 10 ordered score levels.</typeparam>
/// <param name="question">The question Jev should answer.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JevScoreQuestionAttribute<T>(string question) : Attribute
    where T : struct, Enum
{
    /// <summary>
    /// Gets the question Jev should answer.
    /// </summary>
    public string Question { get; } = question;
}
