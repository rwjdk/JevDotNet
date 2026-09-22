# JevDotNet

JevDotNet is an opinionated, convention-based .NET client for the
[TypeSafe AI Jev API](https://docs.typesafe.ai/introduction). It turns Jev Choice,
Score, and Noul answers into a strongly typed result object.

## Installation

```shell
dotnet add package JevDotNet
```

JevDotNet targets .NET 8.

## Quick start

Define an enum for the available choices and a class for the result:

```csharp
using System.ComponentModel;
using JevDotNet;
using JevDotNet.Models;

public enum Category
{
    [Description("Questions about using or troubleshooting the product")]
    Technical,

    [Description("Questions about invoices, payments, or subscriptions")]
    Billing,

    Other
}

public sealed class Classification
{
    public Category? Category { get; set; }

    [JevAnswerFor(nameof(Category))]
    public JevChoice<Category>? CategoryDetails { get; set; }
}
```

Create a client and evaluate some text:

```csharp
var client = new JevClient(Environment.GetEnvironmentVariable("JEV_API_KEY")!);

JevResponse<Classification> response = await client.EvaluateAsync<Classification>(
    "The customer cannot sign in after resetting their password.",
    [new JevChoiceQuestion<Category>("What is the primary category?", "Category")]);

Category? category = response.Result.Category;
double? confidence = response.Result.CategoryDetails?.Confidence;
```

The question ID (`Category` above) maps to a result property with the same name,
ignoring case. Use `[JevAnswerFor("questionId")]` when a property has a different
name or when you also want the detailed response.

## Question types

### Choice

`JevChoiceQuestion<TEnum>` returns one enum member. A member's
`DescriptionAttribute` is sent as its criterion description; otherwise its name is
used. A result can expose the enum value, `JevChoice<TEnum>`, or both.

### Score

`JevScoreQuestion<TEnum>` uses an enum with 2–10 members as an ordered scale. Enum
declaration order defines levels 0, 1, 2, and so on. A result can expose the score
as `double`, `decimal`, their nullable forms, `JevScore<TEnum>`, or any combination.

### Noul

`JevNoulQuestion` returns a probability from 0 to 1. A result can expose it as
`double`, `decimal`, their nullable forms, `JevNoul`, `bool`, or nullable `bool`.
Boolean values default to `true` at 0.5 or above. Override that threshold on a
Boolean property with `[JevNoulThreshold(0.7)]`.

## Client configuration

For custom configuration, construct the client with `JevClientOptions`:

```csharp
var client = new JevClient(new JevClientOptions
{
    ApiKey = apiKey,
    Model = "jev-latest",
    Endpoint = new Uri("https://api.typesafe.ai/v1/systemone"),
    HttpClientFactory = () => new HttpClient()
});
```

The returned `JevResponse<T>` contains the converted `Result` together with the
`InputTokenCount` and `OutputTokenCount` reported by the API.

Keep API keys outside source control, for example in environment variables or
.NET user secrets.
