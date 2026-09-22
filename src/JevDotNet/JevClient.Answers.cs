using System.Collections;
using System.Reflection;
using System.Text.Json;
using JevDotNet.ClassLib.Models;

namespace JevDotNet.ClassLib;

public partial class JevClient
{
    private static T ConvertAnswers<T>(IEnumerable<JevQuestion> questions, JevResponse response)
    {
        T result = Activator.CreateInstance<T>();

        foreach (JevQuestion question in questions)
        {
            if (!response.Answers.TryGetValue(question.Id, out JsonElement answer))
            {
                throw new JsonException($"The API response did not contain an answer for '{question.Id}'.");
            }

            ApplyAnswer(result, question, answer);
        }

        return result;
    }

    private static void ApplyAnswer<T>(T result, JevQuestion question, JsonElement answer)
    {
        Type questionType = question.GetType();

        if (IsGenericType(questionType, typeof(JevChoiceQuestion<>)))
        {
            ApplyChoiceAnswer(result, question, answer, questionType.GetGenericArguments()[0]);
            return;
        }

        if (IsGenericType(questionType, typeof(JevScoreQuestion<>)))
        {
            ApplyScoreAnswer(result, question, answer, questionType.GetGenericArguments()[0]);
            return;
        }

        if (question is JevNoulQuestion)
        {
            ApplyNoulAnswer(result, question, answer);
            return;
        }

        throw new NotSupportedException(
            $"Question type '{questionType.Name}' cannot be converted yet.");
    }

    private static void ApplyChoiceAnswer<T>(T result, JevQuestion question, JsonElement answer, Type enumType)
    {
        EnsureAnswerType(answer, question, "choice");

        PropertyInfo[] valueProperties = FindMappedProperties<T>(question.Id, property =>
            property.PropertyType == enumType || Nullable.GetUnderlyingType(property.PropertyType) == enumType);
        PropertyInfo[] detailsProperties = FindMappedProperties<T>(question.Id, property =>
            IsGenericType(property.PropertyType, typeof(JevChoice<>)));

        EnsureAnyMapping<T>(question, valueProperties, detailsProperties, "value or details");
        EnsureAtMostOne<T>(question, valueProperties, "Choice value");
        EnsureAtMostOne<T>(question, detailsProperties, "Choice details");

        string choice = answer.GetProperty("choice").GetString()
            ?? throw new JsonException($"Choice answer '{question.Id}' has no choice value.");
        if (!Enum.TryParse(enumType, choice, true, out object? enumValue))
        {
            throw new JsonException($"Choice '{choice}' is not valid for enum {enumType.Name}.");
        }

        foreach (PropertyInfo property in valueProperties)
        {
            SetProperty(result, property, enumValue, "Choice");
        }

        if (detailsProperties.Length == 0)
        {
            return;
        }

        Type detailsType = typeof(JevChoice<>).MakeGenericType(enumType);
        PropertyInfo detailsProperty = detailsProperties[0];
        EnsurePropertyType(detailsProperty, detailsType, "Choice details");

        IDictionary probabilities = CreateDictionary(enumType, typeof(double));
        foreach (JsonProperty probability in answer.GetProperty("probabilities").EnumerateObject())
        {
            if (!Enum.TryParse(enumType, probability.Name, true, out object? option))
            {
                throw new JsonException(
                    $"Probability option '{probability.Name}' is not valid for enum {enumType.Name}.");
            }

            probabilities.Add(option, probability.Value.GetDouble());
        }

        object details = Activator.CreateInstance(
            detailsType,
            enumValue,
            answer.GetProperty("confidence").GetDouble(),
            probabilities)!;
        SetProperty(result, detailsProperty, details, "Choice details");
    }

    private static void ApplyScoreAnswer<T>(T result, JevQuestion question, JsonElement answer, Type enumType)
    {
        EnsureAnswerType(answer, question, "score");

        Type[] numericTypes = [typeof(double), typeof(decimal), typeof(double?), typeof(decimal?)];
        PropertyInfo[] valueProperties = FindMappedProperties<T>(question.Id,
            property => numericTypes.Contains(property.PropertyType));
        PropertyInfo[] detailsProperties = FindMappedProperties<T>(question.Id,
            property => IsGenericType(property.PropertyType, typeof(JevScore<>)));

        EnsureAnyMapping<T>(question, valueProperties, detailsProperties, "score value or details");

        JsonElement scoreElement = answer.GetProperty("score");
        foreach (PropertyInfo property in valueProperties)
        {
            Type valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object value = valueType == typeof(decimal) ? scoreElement.GetDecimal() : scoreElement.GetDouble();
            SetProperty(result, property, value, "Score");
        }

        Type detailsType = typeof(JevScore<>).MakeGenericType(enumType);
        foreach (PropertyInfo property in detailsProperties)
        {
            EnsurePropertyType(property, detailsType, "Score details");
            object details = CreateScoreDetails(answer, enumType, detailsType);
            SetProperty(result, property, details, "Score details");
        }
    }

    private static object CreateScoreDetails(JsonElement answer, Type enumType, Type detailsType)
    {
        Array levels = Enum.GetValues(enumType);
        IDictionary probabilities = CreateDictionary(enumType, typeof(double));
        IDictionary legend = CreateDictionary(enumType, typeof(string));

        AddScoreEntries(answer.GetProperty("probabilities"), levels, probabilities, element => element.GetDouble());
        AddScoreEntries(answer.GetProperty("legend"), levels, legend, element =>
            element.GetString() ?? throw new JsonException("A Score legend entry was not a string."));

        return Activator.CreateInstance(
            detailsType,
            answer.GetProperty("score").GetDouble(),
            answer.GetProperty("confidence").GetDouble(),
            probabilities,
            legend)!;
    }

    private static void AddScoreEntries(
        JsonElement entries,
        Array levels,
        IDictionary destination,
        Func<JsonElement, object> getValue)
    {
        foreach (JsonProperty entry in entries.EnumerateObject())
        {
            if (!int.TryParse(entry.Name, out int level) || level < 0 || level >= levels.Length)
            {
                throw new JsonException($"Score level '{entry.Name}' is invalid.");
            }

            destination.Add(levels.GetValue(level)!, getValue(entry.Value));
        }
    }

    private static void ApplyNoulAnswer<T>(T result, JevQuestion question, JsonElement answer)
    {
        EnsureAnswerType(answer, question, "noul");

        Type[] valueTypes =
        [
            typeof(double), typeof(decimal), typeof(double?), typeof(decimal?), typeof(bool), typeof(bool?)
        ];
        PropertyInfo[] valueProperties = FindMappedProperties<T>(question.Id,
            property => valueTypes.Contains(property.PropertyType));
        PropertyInfo[] noulProperties = FindMappedProperties<T>(question.Id,
            property => property.PropertyType == typeof(JevNoul));

        EnsureAnyMapping<T>(question, valueProperties, noulProperties, "Noul value or response");
        EnsureAtMostOne<T>(question, noulProperties, nameof(JevNoul));

        JsonElement noulElement = answer.GetProperty("noul");
        foreach (PropertyInfo property in valueProperties)
        {
            Type valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            JevNoulThresholdAttribute? threshold = property.GetCustomAttribute<JevNoulThresholdAttribute>();
            if (threshold is not null && valueType != typeof(bool))
            {
                throw new InvalidOperationException(
                    $"JevNoulThreshold can only be applied to bool or nullable bool properties; " +
                    $"'{property.Name}' has type {property.PropertyType.Name}.");
            }

            object value = valueType == typeof(bool)
                ? noulElement.GetDouble() >= (threshold?.Threshold ?? 0.5)
                : valueType == typeof(decimal)
                    ? noulElement.GetDecimal()
                    : noulElement.GetDouble();
            SetProperty(result, property, value, "Noul");
        }

        foreach (PropertyInfo property in noulProperties)
        {
            if (property.GetCustomAttribute<JevNoulThresholdAttribute>() is not null)
            {
                throw new InvalidOperationException(
                    $"JevNoulThreshold can only be applied to bool or nullable bool properties; " +
                    $"'{property.Name}' has type {nameof(JevNoul)}.");
            }

            SetProperty(result, property, new JevNoul(noulElement.GetDouble()), nameof(JevNoul));
        }
    }

    private static PropertyInfo[] FindMappedProperties<T>(string questionId, Func<PropertyInfo, bool> predicate) =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(predicate)
            .Where(property => IsMappedTo(property, questionId))
            .ToArray();

    private static bool IsMappedTo(PropertyInfo property, string questionId) =>
        string.Equals(property.Name, questionId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            property.GetCustomAttribute<JevAnswerForAttribute>()?.QuestionId,
            questionId,
            StringComparison.OrdinalIgnoreCase);

    private static void EnsureAnswerType(JsonElement answer, JevQuestion question, string expectedType)
    {
        string actualType = answer.GetProperty("type").GetString()
            ?? throw new JsonException($"Answer '{question.Id}' has no type.");
        if (actualType != expectedType)
        {
            throw new JsonException(
                $"Expected a {expectedType} answer for '{question.Id}', but received '{actualType}'.");
        }
    }

    private static void EnsureAnyMapping<T>(
        JevQuestion question,
        PropertyInfo[] values,
        PropertyInfo[] details,
        string description)
    {
        if (values.Length == 0 && details.Length == 0)
        {
            throw new InvalidOperationException(
                $"No {description} property for question '{question.Id}' exists on {typeof(T).Name}.");
        }
    }

    private static void EnsureAtMostOne<T>(JevQuestion question, PropertyInfo[] properties, string description)
    {
        if (properties.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple {description} properties on {typeof(T).Name} match question '{question.Id}'.");
        }
    }

    private static void EnsurePropertyType(PropertyInfo property, Type expectedType, string description)
    {
        if (property.PropertyType != expectedType)
        {
            throw new InvalidOperationException(
                $"{description} property '{property.Name}' must have type {expectedType.Name}.");
        }
    }

    private static void SetProperty<T>(T result, PropertyInfo property, object value, string description)
    {
        if (!property.CanWrite)
        {
            throw new InvalidOperationException(
                $"{description} property '{property.Name}' on {typeof(T).Name} is not writable.");
        }

        property.SetValue(result, value);
    }

    private static IDictionary CreateDictionary(Type keyType, Type valueType) =>
        (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;
}
