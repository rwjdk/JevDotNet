[![Build and Test](https://img.shields.io/github/actions/workflow/status/rwjdk/jev-dotnet/build-and-test.yml?branch=main&style=for-the-badge)](https://github.com/rwjdk/jev-dotnet/actions/workflows/build-and-test.yml)
[![Bugs](https://img.shields.io/github/issues/rwjdk/jev-dotnet/bug?style=for-the-badge&label=Bugs)](https://github.com/rwjdk/jev-dotnet/issues?q=is%3Aissue%20state%3Aopen%20label%3Abug)
[![Coverage Status](https://img.shields.io/coveralls/github/rwjdk/jev-dotnet?branch=main&style=for-the-badge)](https://coveralls.io/github/rwjdk/jev-dotnet?branch=main)

# JevDotNet

JevDotNet is an opinionated, convention-based .NET client for the [Jev API](https://docs.typesafe.ai/introduction) by TypeSafe AI.

[![NuGet](https://img.shields.io/badge/NuGet-blue?style=for-the-badge)](https://www.nuget.org/packages/JevDotNet)
[![Wiki](https://img.shields.io/badge/Wiki-brown?style=for-the-badge)](https://github.com/rwjdk/jev-dotnet/wiki)
[![Changelog](https://img.shields.io/badge/-Changelog-darkgreen?style=for-the-badge)](https://github.com/rwjdk/jev-dotnet/blob/main/CHANGELOG.md)
[![YouTube](https://img.shields.io/badge/-YouTube-darkred?style=for-the-badge)](https://youtu.be/T6jRVRXtRd8)
[![API Reference](https://img.shields.io/badge/API_Reference-gray?style=for-the-badge)](https://docs.typesafe.ai/introduction)

## Quick start

Watch [this video](https://youtu.be/T6jRVRXtRd8), or follow steps below.

### Step 1
Define your return object and decorate its properties with the Jev attributes (Choice, Score, or Noul). 

- Each attributed property defines one question sent to Jev
- The property types can be either simple or detailed types

```csharp
using System.ComponentModel;
using JevDotNet;
using JevDotNet.Models;

public class MyJevReturnObject
{
    [JevChoiceQuestion<Category>("What is the primary category?")]
    public Category? Category { get; set; }

    [JevScoreQuestion<Severity>("How severe is the reported impact?")]
    public JevScore<Severity>? Severity { get; set; }

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

### Step 2

Create a client and evaluate some text:

```csharp
JevClient client = new JevClient("<api-key>");

string input = "The customer cannot sign in after resetting their password.";
JevResponse<MyJevReturnObject> response = await client.EvaluateAsync<MyJevReturnObject>(input);

MyJevReturnObject myReturnObject = response.Result;

Category? category = myReturnObject.Category;
double? severity = myReturnObject.Severity.Score;
bool? isBroken = myReturnObject.IsBroken;
```

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
