namespace JevDotNet.Models;

/// <summary>
/// Defines a Choice question on a result property.
/// </summary>
/// <typeparam name="T">The enum that defines the available choices.</typeparam>
/// <param name="question">The question Jev should answer.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JevChoiceQuestionAttribute<T>(string question) : Attribute
    where T : struct, Enum
{
    /// <summary>
    /// Gets the question Jev should answer.
    /// </summary>
    public string Question { get; } = question;
}
