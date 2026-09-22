namespace JevDotNet.ClassLib.Models;

public abstract class JevQuestion(string instructions, string questionId)
{
    public string Id { get; } = questionId;
    public string Instructions { get; } = instructions;
}
