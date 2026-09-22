using System.ComponentModel;
using System.Reflection;
using JevDotNet.Models;

namespace JevDotNet;

public partial class JevClient
{
    private static IReadOnlyList<QuestionDefinition> BuildQuestionDefinitions<T>()
    {
        List<QuestionDefinition> definitions = [];
        HashSet<string> questionIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object[] questionAttributes = property.GetCustomAttributes()
                .Where(IsQuestionAttribute)
                .ToArray<object>();

            if (questionAttributes.Length == 0)
            {
                continue;
            }

            if (questionAttributes.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Property '{property.Name}' on {typeof(T).Name} has multiple Jev question attributes.");
            }

            if (!property.CanWrite)
            {
                throw new InvalidOperationException(
                    $"Question property '{property.Name}' on {typeof(T).Name} is not writable.");
            }

            if (!questionIds.Add(property.Name))
            {
                throw new InvalidOperationException(
                    $"Multiple properties on {typeof(T).Name} produce the question ID '{property.Name}'.");
            }

            definitions.Add(BuildQuestionDefinition(property, questionAttributes[0]));
        }

        if (definitions.Count == 0)
        {
            throw new InvalidOperationException(
                $"No Jev question properties were found on {typeof(T).Name}.");
        }

        return definitions;
    }

    private static bool IsQuestionAttribute(object attribute)
    {
        Type attributeType = attribute.GetType();
        return attribute is JevNoulQuestionAttribute ||
               IsGenericType(attributeType, typeof(JevChoiceQuestionAttribute<>)) ||
               IsGenericType(attributeType, typeof(JevScoreQuestionAttribute<>));
    }

    private static QuestionDefinition BuildQuestionDefinition(PropertyInfo property, object attribute)
    {
        Type attributeType = attribute.GetType();
        if (IsGenericType(attributeType, typeof(JevChoiceQuestionAttribute<>)))
        {
            string question = (string)attributeType.GetProperty(nameof(JevChoiceQuestionAttribute<>.Question))!
                .GetValue(attribute)!;
            EnsureQuestion(property, question);
            Type enumType = attributeType.GetGenericArguments()[0];
            ValidateChoiceProperty(property, enumType);
            return new QuestionDefinition(property, QuestionKind.Choice, question, enumType, null);
        }

        if (attribute is JevNoulQuestionAttribute noul)
        {
            EnsureQuestion(property, noul.Question);
            ValidateNoulProperty(property, noul);
            return new QuestionDefinition(property, QuestionKind.Noul, noul.Question, null, noul);
        }

        if (IsGenericType(attributeType, typeof(JevScoreQuestionAttribute<>)))
        {
            string question = (string)attributeType.GetProperty(nameof(JevScoreQuestionAttribute<>.Question))!
                .GetValue(attribute)!;
            EnsureQuestion(property, question);
            Type enumType = attributeType.GetGenericArguments()[0];
            ValidateScoreProperty(property, enumType);
            return new QuestionDefinition(property, QuestionKind.Score, question, enumType, null);
        }

        throw new NotSupportedException($"Attribute '{attributeType.Name}' is not a Jev question attribute.");
    }

    private static void ValidateChoiceProperty(PropertyInfo property, Type enumType)
    {
        Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        bool isMatchingValue = propertyType == enumType;
        bool isMatchingDetails = IsGenericType(propertyType, typeof(JevChoice<>)) &&
                                 propertyType.GetGenericArguments()[0] == enumType;

        if (!isMatchingValue && !isMatchingDetails)
        {
            throw new InvalidOperationException(
                $"Choice property '{property.Name}' must be {enumType.Name}, nullable {enumType.Name}, " +
                $"or {typeof(JevChoice<>).Name} using {enumType.Name}.");
        }
    }

    private static void ValidateScoreProperty(PropertyInfo property, Type enumType)
    {
        Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        bool isNumeric = propertyType == typeof(double) || propertyType == typeof(decimal);
        bool isMatchingDetails = IsGenericType(propertyType, typeof(JevScore<>)) &&
                                 propertyType.GetGenericArguments()[0] == enumType;

        if (!isNumeric && !isMatchingDetails)
        {
            throw new InvalidOperationException(
                $"Score property '{property.Name}' must be double, decimal, a nullable equivalent, or {typeof(JevScore<>).Name} using {enumType.Name}.");
        }
    }

    private static void ValidateNoulProperty(PropertyInfo property, JevNoulQuestionAttribute attribute)
    {
        Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        bool isSupported = propertyType == typeof(double) ||
                           propertyType == typeof(decimal) ||
                           propertyType == typeof(bool) ||
                           propertyType == typeof(JevNoul);

        if (!isSupported)
        {
            throw new InvalidOperationException(
                $"Noul property '{property.Name}' has unsupported type {property.PropertyType.Name}.");
        }

        if (attribute.HasCustomThreshold && propertyType != typeof(bool))
        {
            throw new InvalidOperationException(
                $"A custom Noul threshold can only be used on bool or nullable bool properties; " +
                $"'{property.Name}' has type {property.PropertyType.Name}.");
        }

        if ((attribute.True is null) != (attribute.False is null))
        {
            throw new InvalidOperationException(
                $"Noul property '{property.Name}' must define both True and False criteria or neither.");
        }
    }

    private static void EnsureQuestion(PropertyInfo property, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException(
                $"Question property '{property.Name}' must define a non-empty question.");
        }
    }

    private static Dictionary<string, object> BuildQuestions(IEnumerable<QuestionDefinition> definitions) =>
        definitions.ToDictionary(
            definition => definition.Property.Name,
            BuildQuestion,
            StringComparer.OrdinalIgnoreCase);

    private static object BuildQuestion(QuestionDefinition definition) => definition.Kind switch
    {
        QuestionKind.Choice => BuildChoiceQuestion(definition),
        QuestionKind.Score => BuildScoreQuestion(definition),
        QuestionKind.Noul => BuildNoulQuestion(definition),
        _ => throw new NotSupportedException($"Question kind '{definition.Kind}' is not supported.")
    };

    private static object BuildChoiceQuestion(QuestionDefinition definition)
    {
        Type enumType = definition.EnumType!;
        Dictionary<string, string> criteria = Enum.GetNames(enumType)
            .ToDictionary(name => name, name => GetEnumDescription(enumType, name));

        return new { type = "choice", instructions = definition.Question, criteria };
    }

    private static object BuildScoreQuestion(QuestionDefinition definition)
    {
        Type enumType = definition.EnumType!;
        string[] levelNames = Enum.GetNames(enumType);
        if (levelNames.Length is < 2 or > 10)
        {
            throw new InvalidOperationException(
                $"Score enum {enumType.Name} must define between 2 and 10 levels.");
        }

        string[] criteria = levelNames
            .Select(name => GetEnumDescription(enumType, name))
            .ToArray();

        return new { type = "score", instructions = definition.Question, criteria };
    }

    private static object BuildNoulQuestion(QuestionDefinition definition)
    {
        JevNoulQuestionAttribute attribute = definition.NoulAttribute!;
        return attribute.True is null
            ? new { type = "noul", instructions = definition.Question }
            : new
            {
                type = "noul",
                instructions = definition.Question,
                criteria = new Dictionary<string, string>
                {
                    ["true"] = attribute.True,
                    ["false"] = attribute.False!
                }
            };
    }

    private static string GetEnumDescription(Type enumType, string name) =>
        enumType.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;

    private static bool IsGenericType(Type type, Type genericTypeDefinition) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;

    private sealed record QuestionDefinition(
        PropertyInfo Property,
        QuestionKind Kind,
        string Question,
        Type? EnumType,
        JevNoulQuestionAttribute? NoulAttribute);

    private enum QuestionKind
    {
        Choice,
        Score,
        Noul
    }
}
