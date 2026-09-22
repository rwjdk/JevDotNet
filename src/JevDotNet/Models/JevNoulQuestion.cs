namespace JevDotNet.ClassLib.Models;

public sealed class JevNoulQuestion : JevQuestion
{
    public JevNoulQuestion(
        string instructions,
        string questionId,
        JevNoulCriteria? criteria = null)
        : base(instructions, questionId)
    {
        Criteria = criteria;
    }

    public JevNoulCriteria? Criteria { get; }
}
