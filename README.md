# JevDotNet

A typed .NET client for the [TypeSafe AI Jev API](https://docs.typesafe.ai/introduction).

## Installation

```shell
dotnet add package JevDotNet
```

## Usage

Define the possible choices and the object you want returned:

```csharp
using JevDotNet.ClassLib;
using JevDotNet.ClassLib.Models;

public enum Category
{
    Technical,
    Billing,
    Other
}

public sealed class Classification
{
    public Category Category { get; set; }

    [JevAnswerFor(nameof(Category))]
    public JevChoice<Category>? CategoryDetails { get; set; }
}
```

Evaluate an input with one or more questions:

```csharp
var client = new JevClient(Environment.GetEnvironmentVariable("JEV_API_KEY")!);

JevResponse<Classification> response = await client.EvaluateAsync<Classification>(
    "The customer cannot sign in after resetting their password.",
    [new JevChoiceQuestion<Category>("What is the primary category?", "Category")]);

Classification result = response.Result;
JevResponse raw = response.Raw;
```

The client currently supports Choice, Score, and Noul questions. Enum member
`DescriptionAttribute` values are used as criteria descriptions when present.

## Build and test

```shell
dotnet test
dotnet pack src/JevDotNet/JevDotNet.csproj --configuration Release
```
