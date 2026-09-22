namespace JevDotNet.ClassLib.Models;

public class JevScoreQuestion<T> : JevQuestion where T : struct, Enum
{
    public JevScoreQuestion(string instructions, string questionId) : base(instructions, questionId)
    {
    }
}
