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
                    $"Property '{property.Name}' on {typeof(T).Name} has multiple Jev question attributes. " +
                    $"Keep exactly one Choice, Score, or Noul attribute per question property. See {WikiLinks.ReturnObject}.");
            }

            if (!property.CanWrite)
            {
                throw new InvalidOperationException(
                    $"Question property '{property.Name}' on {typeof(T).Name} is not writable. " +
                    $"Add a public setter. See {WikiLinks.ReturnObject}.");
            }

            if (!questionIds.Add(property.Name))
            {
                throw new InvalidOperationException(
                    $"Multiple properties on {typeof(T).Name} produce the question ID '{property.Name}' " +
                    $"(IDs are case-insensitive). Rename one property. See {WikiLinks.ReturnObject}.");
            }

            definitions.Add(BuildQuestionDefinition(property, questionAttributes[0]));
        }

        if (definitions.Count == 0)
        {
            throw new InvalidOperationException(
                $"No Jev question properties were found on {typeof(T).Name}. Add a public property with " +
                $"JevChoiceQuestion<T>, JevScoreQuestion<T>, or JevNoulQuestion. See {WikiLinks.ReturnObject}.");
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

        throw new NotSupportedException(
            $"Attribute '{attributeType.Name}' is not a supported Jev question attribute. " +
            $"Use a Choice, Score, or Noul question attribute. See {WikiLinks.ReturnObject}.");
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
                $"Choice property '{property.Name}' has type {property.PropertyType.Name}. Use " +
                $"{enumType.Name}, {enumType.Name}?, or JevChoice<{enumType.Name}> to match its attribute. " +
                $"See {WikiLinks.Choice}.");
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
                $"Score property '{property.Name}' has type {property.PropertyType.Name}. Use double, decimal, " +
                $"a nullable equivalent, or JevScore<{enumType.Name}> to match its attribute. " +
                $"See {WikiLinks.Score}.");
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
                $"Noul property '{property.Name}' has unsupported type {property.PropertyType.Name}. " +
                $"Use double, decimal, bool, JevNoul, or a nullable equivalent. See {WikiLinks.Noul}.");
        }

        if (attribute.HasCustomThreshold && propertyType != typeof(bool))
        {
            throw new InvalidOperationException(
                $"Noul property '{property.Name}' uses a custom threshold but has type " +
                $"{property.PropertyType.Name}. Use bool or bool?, or remove the attribute threshold and " +
                $"call JevNoul.IsAtLeast() after evaluation. See {WikiLinks.Noul}.");
        }

        if ((attribute.True is null) != (attribute.False is null))
        {
            throw new InvalidOperationException(
                $"Noul property '{property.Name}' defines only one outcome criterion. " +
                $"Set both True and False, or remove both. See {WikiLinks.Noul}.");
        }
    }

    private static void EnsureQuestion(PropertyInfo property, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException(
                $"Question property '{property.Name}' has an empty question. " +
                $"Provide one specific question in the attribute. See {WikiLinks.ReturnObject}.");
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
        _ => throw new NotSupportedException(
            $"Question kind '{definition.Kind}' is not supported. See {WikiLinks.ReturnObject}.")
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
            string levelWord = levelNames.Length == 1 ? "level" : "levels";
            throw new InvalidOperationException(
                $"Score enum {enumType.Name} defines {levelNames.Length} {levelWord}; it needs 2 to 10. " +
                $"Add or remove enum members. See {WikiLinks.Score}.");
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

    private static string GuideFor(QuestionKind kind) => kind switch
    {
        QuestionKind.Choice => WikiLinks.Choice,
        QuestionKind.Score => WikiLinks.Score,
        QuestionKind.Noul => WikiLinks.Noul,
        _ => WikiLinks.ReturnObject
    };
}
