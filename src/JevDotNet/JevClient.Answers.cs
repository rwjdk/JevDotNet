using System.Collections;
using System.Reflection;
using System.Text.Json;
using JevDotNet.Models;

namespace JevDotNet;

public partial class JevClient
{
    private static T ConvertAnswers<T>(IEnumerable<QuestionDefinition> definitions, JevResponse response)
    {
        T result = Activator.CreateInstance<T>();

        foreach (QuestionDefinition definition in definitions)
        {
            if (!response.Answers.TryGetValue(definition.Property.Name, out JsonElement answer))
            {
                throw new JsonException(
                    $"The API response did not contain an answer for '{definition.Property.Name}'.");
            }

            object value = definition.Kind switch
            {
                QuestionKind.Choice => ConvertChoiceAnswer(definition, answer),
                QuestionKind.Score => ConvertScoreAnswer(definition, answer),
                QuestionKind.Noul => ConvertNoulAnswer(definition, answer),
                _ => throw new NotSupportedException($"Question kind '{definition.Kind}' is not supported.")
            };

            definition.Property.SetValue(result, value);
        }

        return result;
    }

    private static object ConvertChoiceAnswer(QuestionDefinition definition, JsonElement answer)
    {
        EnsureAnswerType(answer, definition, "choice");
        Type enumType = definition.EnumType!;
        string choice = answer.GetProperty("choice").GetString()
            ?? throw new JsonException($"Choice answer '{definition.Property.Name}' has no choice value.");

        if (!TryParseDeclaredEnumName(enumType, choice, out object? enumValue))
        {
            throw new JsonException($"Choice '{choice}' is not valid for enum {enumType.Name}.");
        }

        Type propertyType = Nullable.GetUnderlyingType(definition.Property.PropertyType) ??
                            definition.Property.PropertyType;
        if (!IsGenericType(propertyType, typeof(JevChoice<>)))
        {
            return enumValue!;
        }

        IDictionary probabilities = CreateDictionary(enumType, typeof(double));
        foreach (JsonProperty probability in answer.GetProperty("probabilities").EnumerateObject())
        {
            if (!TryParseDeclaredEnumName(enumType, probability.Name, out object? option))
            {
                throw new JsonException(
                    $"Probability option '{probability.Name}' is not valid for enum {enumType.Name}.");
            }

            if (probabilities.Contains(option!))
            {
                throw new JsonException(
                    $"Choice answer '{definition.Property.Name}' contains duplicate probability options.");
            }

            probabilities.Add(option!, probability.Value.GetDouble());
        }

        return Activator.CreateInstance(
            propertyType,
            enumValue,
            answer.GetProperty("confidence").GetDouble(),
            probabilities)!;
    }

    private static bool TryParseDeclaredEnumName(Type enumType, string name, out object? value)
    {
        string? declaredName = Enum.GetNames(enumType)
            .FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
        if (declaredName is null)
        {
            value = null;
            return false;
        }

        value = Enum.Parse(enumType, declaredName);
        return true;
    }

    private static object ConvertScoreAnswer(QuestionDefinition definition, JsonElement answer)
    {
        EnsureAnswerType(answer, definition, "score");
        Type propertyType = Nullable.GetUnderlyingType(definition.Property.PropertyType) ??
                            definition.Property.PropertyType;
        JsonElement score = answer.GetProperty("score");

        if (propertyType == typeof(decimal))
        {
            return score.GetDecimal();
        }

        if (propertyType == typeof(double))
        {
            return score.GetDouble();
        }

        Type enumType = definition.EnumType!;
        Array levels = Enum.GetValues(enumType);
        IDictionary probabilities = CreateDictionary(enumType, typeof(double));
        IDictionary legend = CreateDictionary(enumType, typeof(string));

        AddScoreEntries(answer.GetProperty("probabilities"), levels, probabilities, element => element.GetDouble());
        AddScoreEntries(answer.GetProperty("legend"), levels, legend, element =>
            element.GetString() ?? throw new JsonException("A Score legend entry was not a string."));

        return Activator.CreateInstance(
            propertyType,
            score.GetDouble(),
            answer.GetProperty("confidence").GetDouble(),
            probabilities,
            legend)!;
    }

    private static object ConvertNoulAnswer(QuestionDefinition definition, JsonElement answer)
    {
        EnsureAnswerType(answer, definition, "noul");
        Type propertyType = Nullable.GetUnderlyingType(definition.Property.PropertyType) ??
                            definition.Property.PropertyType;
        JsonElement noul = answer.GetProperty("noul");

        if (propertyType == typeof(bool))
        {
            return noul.GetDouble() >= definition.NoulAttribute!.Threshold;
        }

        if (propertyType == typeof(decimal))
        {
            return noul.GetDecimal();
        }

        if (propertyType == typeof(double))
        {
            return noul.GetDouble();
        }

        return new JevNoul(noul.GetDouble());
    }

    private static void EnsureAnswerType(
        JsonElement answer,
        QuestionDefinition definition,
        string expectedType)
    {
        string actualType = answer.GetProperty("type").GetString()
            ?? throw new JsonException($"Answer '{definition.Property.Name}' has no type.");
        if (actualType != expectedType)
        {
            throw new JsonException(
                $"Expected a {expectedType} answer for '{definition.Property.Name}', but received '{actualType}'.");
        }
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

    private static IDictionary CreateDictionary(Type keyType, Type valueType) =>
        (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;
}
