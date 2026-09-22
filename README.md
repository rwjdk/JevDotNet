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
    [JevChoiceQuestion<Category>("What is the primary category?")]
    public JevChoice<Category>? Category { get; set; }
}
```

Create a client and evaluate some text:

```csharp
var client = new JevClient(Environment.GetEnvironmentVariable("JEV_API_KEY")!);

JevResponse<Classification> response = await client.EvaluateAsync<Classification>(
    "The customer cannot sign in after resetting their password.");

Category? category = response.Result.Category?.Choice;
double? confidence = response.Result.Category?.Confidence;
```

Each attributed property defines one question. Its property name is used internally
as the question ID, while its property type controls whether the result contains a
simple value or the detailed answer.

## Question types

### Choice

`[JevChoiceQuestion<TEnum>]` returns one enum member. A member's `DescriptionAttribute`
is sent as its criterion description; otherwise its name is used. Use an enum or
nullable enum property for only the selected value:

```csharp
[JevChoiceQuestion<Category>("What is the primary category?")]
public Category? Category { get; set; }
```

Use `JevChoice<TEnum>` when you also need confidence and probabilities.

### Score

`[JevScoreQuestion<TEnum>]` uses an enum with 2–10 members as an ordered scale.
The members are ordered by their underlying numeric values and mapped to levels
0, 1, 2, and so on. Use `double`, `decimal`, or their nullable forms for only the
score:

```csharp
[JevScoreQuestion<Severity>("How severe is this?")]
public double? Severity { get; set; }
```

Use `JevScore<TEnum>` when you also need confidence, probabilities, and the legend.

### Noul

`[JevNoulQuestion]` returns a probability from 0 to 1. Use `double`, `decimal`,
their nullable forms, or `JevNoul` to receive the probability. Boolean properties
default to `true` at 0.5 or above. Configure the inclusive threshold and optional
criteria on the question itself:

```csharp
[JevNoulQuestion(
    "Is the product broken?",
    0.7,
    True = "The product is broken",
    False = "The product works")]
public bool? IsBroken { get; set; }
```

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
