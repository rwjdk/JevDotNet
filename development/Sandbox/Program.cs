using System.ComponentModel;
using JevDotNet;
using JevDotNet.Models;
using Secrets;

Secrets.Secrets secrets = SecretsManager.GetSecrets();

JevClient client = new JevClient(secrets.TypeSafeAIApiKey);

string input = "The customer cannot sign in after resetting their password.";
JevResponse<MyClassification> response = await client.EvaluateAsync<MyClassification>(input);

Console.WriteLine($"Category: {response.Result.Category?.Choice}");
Console.WriteLine($"Confidence: {response.Result.Category?.Confidence:P0}");

internal sealed class MyClassification
{
    [JevChoiceQuestion<Category>("What is the primary category?")]
    public JevChoice<Category>? Category { get; set; }

    [JevNoulQuestion("Did customer fail?", threshold: 0.7)]
    public required bool Fail { get; set; }
}

internal enum Category
{
    [Description("Questions about using or troubleshooting the product")]
    Technical,

    [Description("Questions about invoices, payments, or subscriptions")]
    Billing,

    Other
}
