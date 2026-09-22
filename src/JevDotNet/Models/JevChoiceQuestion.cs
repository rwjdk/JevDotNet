namespace JevDotNet.ClassLib.Models;

public class JevChoiceQuestion<T> : JevQuestion where T : struct, Enum
{
    public JevChoiceQuestion(string instructions, string questionId) : base(instructions, questionId)
    {
    }
}
