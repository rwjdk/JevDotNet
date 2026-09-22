using Microsoft.Extensions.Configuration;

namespace Secrets;

public static class SecretsManager
{
    public static Secrets GetSecrets()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(SecretsManager).Assembly)
            .Build();

        return new Secrets(configuration["TypeSafeApiKey"] ?? string.Empty);
    }
}
