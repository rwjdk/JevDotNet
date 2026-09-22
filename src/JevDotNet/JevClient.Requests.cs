using System.ComponentModel;
using System.Reflection;
using JevDotNet.Models;

namespace JevDotNet;

public partial class JevClient
{
    private static Dictionary<string, object> BuildQuestions(IEnumerable<JevQuestion> questions)
    {
        Dictionary<string, object> result = [];

        foreach (JevQuestion question in questions)
        {
            result.Add(question.Id, BuildQuestion(question));
        }

        return result;
    }

    private static object BuildQuestion(JevQuestion question)
    {
        Type questionType = question.GetType();

        if (IsGenericType(questionType, typeof(JevChoiceQuestion<>)))
        {
            return BuildChoiceQuestion(question, questionType.GetGenericArguments()[0]);
        }

        if (IsGenericType(questionType, typeof(JevScoreQuestion<>)))
        {
            return BuildScoreQuestion(question, questionType.GetGenericArguments()[0]);
        }

        if (question is JevNoulQuestion noulQuestion)
        {
            return BuildNoulQuestion(noulQuestion);
        }

        throw new NotSupportedException(
            $"Question type '{questionType.Name}' cannot be serialized yet.");
    }

    private static object BuildChoiceQuestion(JevQuestion question, Type enumType)
    {
        Dictionary<string, string> criteria = Enum.GetNames(enumType)
            .ToDictionary(name => name, name => GetEnumDescription(enumType, name));

        return new
        {
            type = "choice",
            instructions = question.Instructions,
            criteria
        };
    }

    private static object BuildScoreQuestion(JevQuestion question, Type enumType)
    {
        string[] levelNames = Enum.GetNames(enumType);
        if (levelNames.Length is < 2 or > 10)
        {
            throw new InvalidOperationException(
                $"Score enum {enumType.Name} must define between 2 and 10 levels.");
        }

        string[] criteria = levelNames
            .Select(name => GetEnumDescription(enumType, name))
            .ToArray();

        return new
        {
            type = "score",
            instructions = question.Instructions,
            criteria
        };
    }

    private static object BuildNoulQuestion(JevNoulQuestion question) =>
        question.Criteria is null
            ? new { type = "noul", instructions = question.Instructions }
            : new { type = "noul", instructions = question.Instructions, criteria = question.Criteria };

    private static string GetEnumDescription(Type enumType, string name) =>
        enumType.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;

    private static bool IsGenericType(Type type, Type genericTypeDefinition) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
}
