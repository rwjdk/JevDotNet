using Microsoft.Extensions.DependencyInjection;

namespace JevDotNet;

/// <summary>
/// Adds a Jev client to a dependency injection service collection.
/// </summary>
public static class JevServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="JevClient"/> with the default model and endpoint.
    /// </summary>
    /// <param name="services">The services to configure.</param>
    /// <param name="apiKey">The TypeSafe AI API key.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJevClient(this IServiceCollection services, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton(new JevClient(apiKey));
    }

    /// <summary>
    /// Registers a singleton <see cref="JevClient"/> with custom options.
    /// </summary>
    /// <param name="services">The services to configure.</param>
    /// <param name="options">The API and HTTP client configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJevClient(this IServiceCollection services, JevClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton(new JevClient(options));
    }
}
