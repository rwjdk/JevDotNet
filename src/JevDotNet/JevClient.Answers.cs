using System.Collections;
using System.Reflection;
using System.Text.Json;
using JevDotNet.Models;

namespace JevDotNet;

public partial class JevClient
{
    private static T ConvertAnswers<T>(IEnumerable<QuestionDefinition> definitions, JevResponse response)
    {
        T result;
        try
        {
            result = Activator.CreateInstance<T>();
        }
        catch (MissingMethodException exception)
        {
            throw new InvalidOperationException(
                $"Return type {typeof(T).Name} needs a public parameterless constructor so JevDotNet can " +
                $"create it. See {WikiLinks.ReturnObject}.", exception);
        }

        foreach (QuestionDefinition definition in definitions)
        {
            if (!response.Answers.TryGetValue(definition.Property.Name, out JsonElement answer))
            {
                throw new JsonException(
                    $"The API response has no answer for question ID '{definition.Property.Name}' on " +
                    $"{typeof(T).Name}. Check that the response uses the property name as the ID. " +
                    $"See {WikiLinks.Troubleshooting}.");
            }

            object value = definition.Kind switch
            {
                QuestionKind.Choice => ConvertChoiceAnswer(definition, answer),
                QuestionKind.Score => ConvertScoreAnswer(definition, answer),
                QuestionKind.Noul => ConvertNoulAnswer(definition, answer),
                _ => throw new NotSupportedException(
                    $"Question kind '{definition.Kind}' is not supported. See {WikiLinks.ReturnObject}.")
            };

            definition.Property.SetValue(result, value);
        }

        return result;
    }

    private static object ConvertChoiceAnswer(QuestionDefinition definition, JsonElement answer)
    {
        EnsureAnswerType(answer, definition, "choice");
        Type enumType = definition.EnumType!;
        string choice = ReadString(
            GetRequiredAnswerField(answer, definition, "choice"), definition, "choice");

        if (!TryParseDeclaredEnumName(enumType, choice, out object? enumValue))
        {
            throw new JsonException(
                $"Choice '{choice}' is not valid for enum {enumType.Name} on question " +
                $"'{definition.Property.Name}'. Check the declared enum member names. See {WikiLinks.Choice}.");
        }

        Type propertyType = Nullable.GetUnderlyingType(definition.Property.PropertyType) ??
                            definition.Property.PropertyType;
        if (!IsGenericType(propertyType, typeof(JevChoice<>)))
        {
            return enumValue!;
        }

        IDictionary probabilities = CreateDictionary(enumType, typeof(double));
        foreach (JsonProperty probability in GetRequiredAnswerField(
                     answer, definition, "probabilities", JsonValueKind.Object).EnumerateObject())
        {
            if (!TryParseDeclaredEnumName(enumType, probability.Name, out object? option))
            {
                throw new JsonException(
                    $"Probability option '{probability.Name}' is not valid for enum {enumType.Name} on " +
                    $"Choice question '{definition.Property.Name}'. See {WikiLinks.Choice}.");
            }

            if (probabilities.Contains(option!))
            {
                throw new JsonException(
                    $"Choice answer '{definition.Property.Name}' contains duplicate probability option " +
                    $"'{probability.Name}'. Check the response option names. See {WikiLinks.Choice}.");
            }

            probabilities.Add(
                option!,
                ReadDouble(probability.Value, definition, $"probabilities['{probability.Name}']"));
        }

        return Activator.CreateInstance(
            propertyType,
            enumValue,
            ReadDouble(GetRequiredAnswerField(answer, definition, "confidence"), definition, "confidence"),
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
        JsonElement score = GetRequiredAnswerField(answer, definition, "score");

        if (propertyType == typeof(decimal))
        {
            return ReadDecimal(score, definition, "score");
        }

        if (propertyType == typeof(double))
        {
            return ReadDouble(score, definition, "score");
        }

        Type enumType = definition.EnumType!;
        Array levels = Enum.GetValues(enumType);
        IDictionary probabilities = CreateDictionary(enumType, typeof(double));
        IDictionary legend = CreateDictionary(enumType, typeof(string));

        AddScoreEntries(
            GetRequiredAnswerField(answer, definition, "probabilities", JsonValueKind.Object), levels, probabilities,
            definition, "probabilities", entry => ReadDouble(
                entry.Value, definition, $"probabilities['{entry.Name}']"));
        AddScoreEntries(
            GetRequiredAnswerField(answer, definition, "legend", JsonValueKind.Object), levels, legend,
            definition, "legend", entry => ReadString(
                entry.Value, definition, $"legend['{entry.Name}']"));

        return Activator.CreateInstance(
            propertyType,
            ReadDouble(score, definition, "score"),
            ReadDouble(GetRequiredAnswerField(answer, definition, "confidence"), definition, "confidence"),
            probabilities,
            legend)!;
    }

    private static object ConvertNoulAnswer(QuestionDefinition definition, JsonElement answer)
    {
        EnsureAnswerType(answer, definition, "noul");
        Type propertyType = Nullable.GetUnderlyingType(definition.Property.PropertyType) ??
                            definition.Property.PropertyType;
        JsonElement noul = GetRequiredAnswerField(answer, definition, "noul");

        if (propertyType == typeof(bool))
        {
            return ReadDouble(noul, definition, "noul") >= definition.NoulAttribute!.Threshold;
        }

        if (propertyType == typeof(decimal))
        {
            return ReadDecimal(noul, definition, "noul");
        }

        if (propertyType == typeof(double))
        {
            return ReadDouble(noul, definition, "noul");
        }

        return new JevNoul(ReadDouble(noul, definition, "noul"));
    }

    private static void EnsureAnswerType(
        JsonElement answer,
        QuestionDefinition definition,
        string expectedType)
    {
        string actualType = ReadString(
            GetRequiredAnswerField(answer, definition, "type"), definition, "type");
        if (actualType != expectedType)
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' has type '{actualType}', but its property expects " +
                $"'{expectedType}'. Check the question attribute and API response. " +
                $"See {GuideFor(definition.Kind)}.");
        }
    }

    private static JsonElement GetRequiredAnswerField(
        JsonElement answer,
        QuestionDefinition definition,
        string field,
        JsonValueKind? expectedKind = null)
    {
        if (answer.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' must be a JSON object for {definition.Kind}. " +
                $"Check the API response shape. See {GuideFor(definition.Kind)}.");
        }

        if (!answer.TryGetProperty(field, out JsonElement value))
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' is missing required '{field}' field for " +
                $"{definition.Kind}. Check the API response shape. See {GuideFor(definition.Kind)}.");
        }

        if (expectedKind is not null && value.ValueKind != expectedKind)
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' field '{field}' must be a JSON " +
                $"{expectedKind.Value.ToString().ToLowerInvariant()} for {definition.Kind}. " +
                $"See {GuideFor(definition.Kind)}.");
        }

        return value;
    }

    private static string ReadString(JsonElement value, QuestionDefinition definition, string field)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' field '{field}' must be a string. " +
                $"See {GuideFor(definition.Kind)}.");
        }

        return value.GetString()!;
    }

    private static double ReadDouble(JsonElement value, QuestionDefinition definition, string field)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number))
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' field '{field}' must be a number. " +
                $"See {GuideFor(definition.Kind)}.");
        }

        return number;
    }

    private static decimal ReadDecimal(JsonElement value, QuestionDefinition definition, string field)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out decimal number))
        {
            throw new JsonException(
                $"Answer '{definition.Property.Name}' field '{field}' must be a decimal number. " +
                $"See {GuideFor(definition.Kind)}.");
        }

        return number;
    }

    private static void AddScoreEntries(
        JsonElement entries,
        Array levels,
        IDictionary destination,
        QuestionDefinition definition,
        string field,
        Func<JsonProperty, object> getValue)
    {
        foreach (JsonProperty entry in entries.EnumerateObject())
        {
            if (!int.TryParse(entry.Name, out int level) || level < 0 || level >= levels.Length)
            {
                throw new JsonException(
                    $"Score answer '{definition.Property.Name}' has invalid {field} level '{entry.Name}'. " +
                    $"Expected an index from 0 to {levels.Length - 1}. See {WikiLinks.Score}.");
            }

            object levelKey = levels.GetValue(level)!;
            if (destination.Contains(levelKey))
            {
                throw new JsonException(
                    $"Score answer '{definition.Property.Name}' repeats {field} level '{entry.Name}'. " +
                    $"Each level needs one entry. See {WikiLinks.Score}.");
            }

            destination.Add(levelKey, getValue(entry));
        }
    }

    private static IDictionary CreateDictionary(Type keyType, Type valueType) =>
        (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;
}
