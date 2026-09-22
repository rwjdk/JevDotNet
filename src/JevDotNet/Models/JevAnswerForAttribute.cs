namespace JevDotNet.ClassLib.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JevAnswerForAttribute(string questionId) : Attribute
{
    public string QuestionId { get; } = questionId;
}
