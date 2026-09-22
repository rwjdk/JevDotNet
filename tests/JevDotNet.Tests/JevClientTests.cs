using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using JevDotNet.Models;

namespace JevDotNet.Tests;

public sealed class JevClientTests
{
    [Fact]
    public async Task EvaluateAsync_MapsAllSupportedAnswerShapes()
    {
        StubHttpMessageHandler handler = new StubHttpMessageHandler(ResponseJson);
        JevClient client = new JevClient(new JevClientOptions
        {
            ApiKey = "test-api-key",
            Model = "jev-test",
            Endpoint = new Uri("https://example.test/evaluate"),
            HttpClientFactory = () => new HttpClient(handler)
        });

        JevResponse<TestResult> response = await client.EvaluateAsync<TestResult>(
            "The service is unavailable.");

        Assert.Equal(Category.Problem, response.Result.CategoryDetails?.Choice);
        Assert.Equal(0.86, response.Result.CategoryDetails?.Confidence);
        Assert.Equal(0.86, response.Result.CategoryDetails?.Probabilities[Category.Problem]);

        Assert.Equal(1.75, response.Result.SeverityDetails?.Score);
        Assert.Equal("Blocking", response.Result.SeverityDetails?.Legend[Severity.Blocking]);

        Assert.True(response.Result.IsBroken);

        Assert.Equal(12, response.InputTokenCount);
        Assert.Equal(6, response.OutputTokenCount);

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
            root.GetProperty("questions").GetProperty("CategoryDetails")
                .GetProperty("criteria").GetProperty("Problem").GetString());
        Assert.Equal(
            "The service is broken",
            root.GetProperty("questions").GetProperty("IsBroken")
                .GetProperty("criteria").GetProperty("true").GetString());
    }

    [Fact]
    public async Task EvaluateAsync_RejectsUndeclaredNumericChoice()
    {
        StubHttpMessageHandler handler = new StubHttpMessageHandler(
            ResponseJson.Replace("\"choice\": \"Problem\"", "\"choice\": \"99\""));
        JevClient client = CreateClient(handler);

        JsonException exception = await Assert.ThrowsAsync<JsonException>(
            () => client.EvaluateAsync<TestResult>("The service is unavailable."));

        Assert.Contains("not valid for enum", exception.Message);
    }

    [Fact]
    public async Task EvaluateAsync_MapsSimpleAndDetailedPropertyShapes()
    {
        StubHttpMessageHandler handler = new StubHttpMessageHandler(PropertyShapesResponseJson);
        JevClient client = CreateClient(handler);

        JevResponse<PropertyShapesResult> response = await client.EvaluateAsync<PropertyShapesResult>(
            "The service is unavailable.");

        Assert.Equal(Category.Problem, response.Result.Category);
        Assert.Equal(1.75m, response.Result.Severity);
        Assert.Equal(0.64, response.Result.IsBroken?.Probability);
        Assert.True(response.Result.DefaultThreshold);
        Assert.True(response.Result.CustomThreshold);
    }

    private static JevClient CreateClient(StubHttpMessageHandler handler) => new(new JevClientOptions
    {
        ApiKey = "test-api-key",
        Model = "jev-test",
        Endpoint = new Uri("https://example.test/evaluate"),
        HttpClientFactory = () => new HttpClient(handler)
    });

    private const string ResponseJson = """
        {
          "model": "jev-response-model",
          "answers": {
            "CategoryDetails": {
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
            "SeverityDetails": {
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

    private const string PropertyShapesResponseJson = """
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
              "probabilities": { "0": 0.00, "1": 0.25, "2": 0.75 },
              "legend": { "0": "Cosmetic", "1": "Degraded", "2": "Blocking" }
            },
            "IsBroken": {
              "type": "noul",
              "noul": 0.64
            },
            "DefaultThreshold": {
              "type": "noul",
              "noul": 0.5
            },
            "CustomThreshold": {
              "type": "noul",
              "noul": 0.7
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
        [JevChoiceQuestion<Category>("What is the category?")]
        public JevChoice<Category>? CategoryDetails { get; set; }

        [JevScoreQuestion<Severity>("How severe is it?")]
        public JevScore<Severity>? SeverityDetails { get; set; }

        [JevNoulQuestion(
            "Is the service broken?",
            0.7,
            True = "The service is broken",
            False = "The service works")]
        public bool? IsBroken { get; set; }
    }

    private sealed class PropertyShapesResult
    {
        [JevChoiceQuestion<Category>("What is the category?")]
        public Category? Category { get; set; }

        [JevScoreQuestion<Severity>("How severe is it?")]
        public decimal? Severity { get; set; }

        [JevNoulQuestion("Is the service broken?")]
        public JevNoul? IsBroken { get; set; }

        [JevNoulQuestion("Does the probability meet the default threshold?")]
        public bool? DefaultThreshold { get; set; }

        [JevNoulQuestion("Does the probability meet the custom threshold?", 0.7)]
        public bool? CustomThreshold { get; set; }
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
