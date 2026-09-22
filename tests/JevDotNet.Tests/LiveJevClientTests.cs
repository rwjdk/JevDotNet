using System.ComponentModel;
using JevDotNet.Models;
using Secrets;

namespace JevDotNet.Tests;

public sealed class LiveJevClientTests
{
    [Fact]
    public async Task EvaluateAsync_MapsChoiceScoreAndNoulFromLiveApi()
    {
        Secrets.Secrets secrets = SecretsManager.GetSecrets();
        Assert.False(
            string.IsNullOrWhiteSpace(secrets.TypeSafeAIApiKey),
            "Configure TypeSafeApiKey in the shared .NET user-secrets store before running live tests.");

        JevClient client = new JevClient(secrets.TypeSafeAIApiKey);

        JevResponse<LiveResult> response = await client.EvaluateAsync<LiveResult>(
            "Our production website is unavailable for every customer.");

        Assert.True(response.Result.Category.HasValue);
        Assert.True(Enum.IsDefined(response.Result.Category.Value));
        Assert.NotNull(response.Result.SeverityDetails);
        Assert.NotNull(response.Result.IsBrokenDetails);
        Assert.InRange(response.Result.IsBrokenDetails.Probability, 0, 1);
        Assert.True(response.InputTokenCount > 0);
        Assert.True(response.OutputTokenCount > 0);
    }

    private enum Category
    {
        Technical,
        Billing,

        [Description("A product or service is not working")]
        Problem,

        Other
    }

    private enum Severity
    {
        Cosmetic,
        Degraded,
        Blocking
    }

    private sealed class LiveResult
    {
        [JevChoiceQuestion<Category>("What is the primary category?")]
        public Category? Category { get; set; }

        [JevScoreQuestion<Severity>("How severe is the reported impact?")]
        public JevScore<Severity>? SeverityDetails { get; set; }

        [JevNoulQuestion(
            "Is the product broken or unavailable?",
            True = "The product is broken or unavailable",
            False = "The product works")]
        public JevNoul? IsBrokenDetails { get; set; }
    }
}
