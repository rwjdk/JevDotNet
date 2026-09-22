using System.ComponentModel;
using JevDotNet;
using JevDotNet.Models;
using Secrets;

Secrets.Secrets secrets = SecretsManager.GetSecrets();
if (string.IsNullOrWhiteSpace(secrets.TypeSafeAIApiKey))
{
    Console.WriteLine(
        "Configure the TypeSafeApiKey user secret before running the sandbox.");
    return;
}

JevClient client = new JevClient(secrets.TypeSafeAIApiKey);
JevResponse<Classification> response = await client.EvaluateAsync<Classification>(
    "The customer cannot sign in after resetting their password.",
    [new JevChoiceQuestion<Category>("What is the primary category?", "Category")]);

Console.WriteLine($"Category: {response.Result.Category}");
Console.WriteLine($"Confidence: {response.Result.CategoryDetails?.Confidence:P0}");

internal enum Category
{
    [Description("Questions about using or troubleshooting the product")]
    Technical,

    [Description("Questions about invoices, payments, or subscriptions")]
    Billing,

    Other
}

internal sealed class Classification
{
    public Category? Category { get; set; }

    [JevAnswerFor(nameof(Category))]
    public JevChoice<Category>? CategoryDetails { get; set; }
}
