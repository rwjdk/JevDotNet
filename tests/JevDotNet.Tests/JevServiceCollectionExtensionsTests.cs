using Microsoft.Extensions.DependencyInjection;

namespace JevDotNet.Tests;

public sealed class JevServiceCollectionExtensionsTests
{
    [Fact]
    public void AddJevClient_WithApiKey_RegistersSingleton()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddJevClient("test-api-key");

        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        JevClient client = provider.GetRequiredService<JevClient>();
        Assert.Same(client, provider.GetRequiredService<JevClient>());
    }

    [Fact]
    public void AddJevClient_WithOptions_UsesConfiguredHttpClientFactory()
    {
        ServiceCollection services = new();
        using HttpClient httpClient = new();
        int factoryCalls = 0;
        JevClientOptions options = new()
        {
            ApiKey = "test-api-key",
            HttpClientFactory = () =>
            {
                factoryCalls++;
                return httpClient;
            }
        };

        services.AddJevClient(options);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<JevClient>());
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void AddJevClient_RejectsMissingApiKey()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentException>(() => services.AddJevClient(string.Empty));
    }
}
