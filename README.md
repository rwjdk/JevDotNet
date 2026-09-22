# JevDotNet

JevDotNet is an opinionated, convention-based .NET client for the [TypeSafe AI Jev API](https://docs.typesafe.ai/introduction).
It turns Jev Choice, Score, and Noul answers into a strongly typed result object.

## YouTube video how to use
https://youtu.be/T6jRVRXtRd8

## Installation

JevDotNet is available on [NuGet](https://www.nuget.org/packages/JevDotNet).

```shell
dotnet add package JevDotNet
```

JevDotNet targets .NET 8 or higher.

## Quick start

Step 1: Define your return object and decorate its properties with the Jev attributes (Choice, Score, or Noul). The property types can be either simple or detailed types (see below):

```csharp
using System.ComponentModel;
using JevDotNet;
using JevDotNet.Models;

public sealed class MyJevReturnObject
{
    [JevChoiceQuestion<Category>("What is the primary category?")]
    public Category? Category { get; set; }

    [JevScoreQuestion<Severity>("How severe is the reported impact?")]
    public double? Severity { get; set; }

    [JevNoulQuestion("Is the product broken?", threshold: 0.7)]
    public bool? IsBroken { get; set; }
}

public enum Category
{
    [Description("Questions about using or troubleshooting the product")]
    Technical,

    [Description("Questions about invoices, payments, or subscriptions")]
    Billing,

    Other
}

public enum Severity
{
    Cosmetic,
    Degraded,
    Blocking
}
```

Step 2: Create a client and evaluate some text:

```csharp
JevClient client = new JevClient(Environment.GetEnvironmentVariable("JEV_API_KEY")!);

string input = "The customer cannot sign in after resetting their password.";
JevResponse<MyJevReturnObject> response = await client.EvaluateAsync<MyJevReturnObject>(input);

MyJevReturnObject myReturnObject = response.Result;

Category? category = myReturnObject.Category;
double? severity = myReturnObject.Severity;
bool? isBroken = myReturnObject.IsBroken;
```

Each attributed property defines one question.

## Question types

### Choice

`[JevChoiceQuestion<TEnum>]` returns one enum member:

```csharp
[JevChoiceQuestion<Category>("What is the primary category?")]
public Category? Category { get; set; }
```

or

```csharp
[JevChoiceQuestion<Category>("What is the primary category?")]
public JevChoice<Category>? Category { get; set; }
```

### Score

`[JevScoreQuestion<TEnum>]` uses an enum with 2–10 members as an ordered scale.
The members are ordered by their underlying numeric values and mapped to levels
0, 1, 2, and so on. Use `double`, `decimal`, or their nullable forms for only the
score:

```csharp
[JevScoreQuestion<Severity>("How severe is this?")]
public double? Severity { get; set; }
```

or

```csharp
[JevScoreQuestion<Severity>("How severe is this?")]
public JevScore<Severity>? Severity { get; set; }
```

### Noul

`[JevNoulQuestion]` returns a probability from 0 to 1. Use `double`, `decimal` or `JevNoul` to receive the probability.

Boolean properties default to `true` at 0.5 or above. Configure the inclusive threshold on the question itself:

```csharp
[JevNoulQuestion("Is the product broken?", threshold: 0.7)]
public bool? IsBroken { get; set; }
```

## Custom client configuration

For custom configuration, construct the client with `JevClientOptions`:

```csharp
HttpClient httpClient = new HttpClient();

var client = new JevClient(new JevClientOptions
{
    ApiKey = apiKey,
    Model = "jev-latest",
    Endpoint = new Uri("https://api.typesafe.ai/v1/systemone"),
    HttpClientFactory = () => httpClient
});
```

The returned `JevResponse<T>` contains the converted `Result` together with the
`InputTokenCount` and `OutputTokenCount` reported by the API.
