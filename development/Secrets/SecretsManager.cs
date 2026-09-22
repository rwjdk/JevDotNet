using Microsoft.Extensions.Configuration;

namespace Secrets;

public static class SecretsManager
{
    public static Secrets GetSecrets()
    {
        string? apiKey = Environment.GetEnvironmentVariable("TypeSafeApiKey");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return new Secrets(apiKey);
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(SecretsManager).Assembly)
            .Build();

        return new Secrets(configuration["TypeSafeApiKey"] ?? string.Empty);
    }
}
