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
            "Our production website is unavailable for every customer.",
            [
                new JevChoiceQuestion<Category>("What is the primary category?", "Category"),
                new JevScoreQuestion<Severity>("How severe is the reported impact?", "Severity"),
                new JevNoulQuestion(
                    "Is the product broken or unavailable?",
                    "IsBroken",
                    new JevNoulCriteria("The product is broken or unavailable", "The product works"))
            ]);

        Assert.True(response.Result.Category.HasValue);
        Assert.True(Enum.IsDefined(response.Result.Category.Value));
        Assert.NotNull(response.Result.CategoryDetails);
        Assert.InRange(response.Result.CategoryDetails.Confidence, 0, 1);
        Assert.True(response.Result.Severity.HasValue);
        Assert.InRange(response.Result.Severity.Value, 0, 2);
        Assert.NotNull(response.Result.SeverityDetails);
        Assert.True(response.Result.IsBrokenProbability.HasValue);
        Assert.InRange(response.Result.IsBrokenProbability.Value, 0, 1);
        Assert.NotNull(response.Result.IsBrokenDetails);
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
        public Category? Category { get; set; }

        [JevAnswerFor(nameof(Category))]
        public JevChoice<Category>? CategoryDetails { get; set; }

        public double? Severity { get; set; }

        [JevAnswerFor(nameof(Severity))]
        public JevScore<Severity>? SeverityDetails { get; set; }

        [JevAnswerFor("IsBroken")]
        public double? IsBrokenProbability { get; set; }

        [JevAnswerFor("IsBroken")]
        public JevNoul? IsBrokenDetails { get; set; }
    }
}
