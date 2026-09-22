using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using JevDotNet.ClassLib;
using JevDotNet.ClassLib.Models;

namespace JevDotNet.Tests;

public sealed class JevClientTests
{
    [Fact]
    public async Task EvaluateAsync_MapsAllSupportedAnswerShapes()
    {
        var handler = new StubHttpMessageHandler(ResponseJson);
        var client = new JevClient(new JevClientOptions
        {
            ApiKey = "test-api-key",
            Model = "jev-test",
            Endpoint = new Uri("https://example.test/evaluate"),
            HttpClientFactory = () => new HttpClient(handler)
        });

        JevResponse<TestResult> response = await client.EvaluateAsync<TestResult>(
            "The service is unavailable.",
            [
                new JevChoiceQuestion<Category>("What is the category?", "Category"),
                new JevScoreQuestion<Severity>("How severe is it?", "Severity"),
                new JevNoulQuestion("Is the service broken?", "IsBroken")
            ]);

        Assert.Equal(Category.Problem, response.Result.Category);
        Assert.Equal(Category.Problem, response.Result.CategoryDetails?.Choice);
        Assert.Equal(0.86, response.Result.CategoryDetails?.Confidence);
        Assert.Equal(0.86, response.Result.CategoryDetails?.Probabilities[Category.Problem]);

        Assert.Equal(1.75, response.Result.Severity);
        Assert.Equal(1.75m, response.Result.SeverityDecimal);
        Assert.Equal(1.75, response.Result.SeverityDetails?.Score);
        Assert.Equal("Blocking", response.Result.SeverityDetails?.Legend[Severity.Blocking]);

        Assert.Equal(0.95, response.Result.IsBrokenProbability);
        Assert.Equal(0.95m, response.Result.IsBrokenDecimal);
        Assert.True(response.Result.IsBroken);
        Assert.Equal(0.95, response.Result.IsBrokenDetails?.Probability);
        Assert.True(response.Result.IsBrokenDetails?.Value);

        Assert.Equal("jev-response-model", response.Raw.Model);
        Assert.Equal(12, response.Usage.InputTokens);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);
        Assert.Equal("test-api-key", handler.Request.Headers.Authorization?.Parameter);

        using JsonDocument request = JsonDocument.Parse(handler.RequestBody!);
        JsonElement root = request.RootElement;
        Assert.Equal("jev-test", root.GetProperty("model").GetString());
        Assert.Equal("The service is unavailable.", root.GetProperty("state").GetString());
        Assert.Equal(
            "A product or service is not working",
            root.GetProperty("questions").GetProperty("Category")
                .GetProperty("criteria").GetProperty("Problem").GetString());
    }

    private const string ResponseJson = """
        {
          "model": "jev-response-model",
          "answers": {
            "Category": {
              "type": "choice",
              "choice": "Problem",
              "confidence": 0.86,
              "probabilities": {
                "Technical": 0.10,
                "Problem": 0.86,
                "Billing": 0.02,
                "Other": 0.02
              }
            },
            "Severity": {
              "type": "score",
              "score": 1.75,
              "confidence": 0.99,
              "probabilities": {
                "0": 0.00,
                "1": 0.25,
                "2": 0.75
              },
              "legend": {
                "0": "Cosmetic",
                "1": "Degraded",
                "2": "Blocking"
              }
            },
            "IsBroken": {
              "type": "noul",
              "noul": 0.95
            }
          },
          "usage": {
            "input_tokens": 12,
            "output_tokens": 6
          }
        }
        """;

    private enum Category
    {
        Technical,

        [Description("A product or service is not working")]
        Problem,

        Billing,
        Other
    }

    private enum Severity
    {
        Cosmetic,
        Degraded,
        Blocking
    }

    private sealed class TestResult
    {
        public Category? Category { get; set; }

        [JevAnswerFor(nameof(Category))]
        public JevChoice<Category>? CategoryDetails { get; set; }

        public double? Severity { get; set; }

        [JevAnswerFor(nameof(Severity))]
        public decimal? SeverityDecimal { get; set; }

        [JevAnswerFor(nameof(Severity))]
        public JevScore<Severity>? SeverityDetails { get; set; }

        [JevAnswerFor("IsBroken")]
        public double? IsBrokenProbability { get; set; }

        [JevAnswerFor("IsBroken")]
        public decimal? IsBrokenDecimal { get; set; }

        [JevAnswerFor("IsBroken")]
        [JevNoulThreshold(0.7)]
        public bool? IsBroken { get; set; }

        [JevAnswerFor("IsBroken")]
        public JevNoul? IsBrokenDetails { get; set; }
    }

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            Request = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                Request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
