using KinaUna.OpenIddict.Services.Interfaces;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    public class ConfigurationClientConfigProvider(IConfiguration configuration) : IClientConfigProvider
    {
        public IEnumerable<ClientConfig> GetClientConfigs()
        {
            string webServerUrl = configuration.GetValue<string>("WebServer") ?? 
                throw new InvalidOperationException("WebServer not found in configuration data.");
            string webServerAzureUrl = configuration.GetValue<string>("WebServerAzure") ?? 
                throw new InvalidOperationException("WebServerAzure not found in configuration data.");
            string webServerLocal = configuration.GetValue<string>("WebServerLocal") ?? 
                throw new InvalidOperationException("WebServerLocal not found in configuration data.");
            string secretString = configuration.GetValue<string>("OpenIddictSecretString") ?? 
                throw new InvalidOperationException("OpenIddictSecretString not found in configuration data.");

            return new[]
            {
                new ClientConfig
                {
                    ClientId = "kinaunawebclient",
                    DisplayName = "KinaUnaWeb",
                    BaseUrl = webServerUrl,
                    ConsentType = ConsentTypes.Implicit,
                    Secret = secretString
                },
                new ClientConfig
                {
                    ClientId = "kinaunawebclientlocal",
                    DisplayName = "KinaUnaWebLocal",
                    BaseUrl = webServerLocal,
                    ConsentType = ConsentTypes.Implicit,
                    Secret = secretString
                },
                new ClientConfig
                {
                    ClientId = "kinaunawebclientAzure",
                    DisplayName = "KinaUnaWebAzure",
                    BaseUrl = webServerAzureUrl,
                    ConsentType = ConsentTypes.Implicit,
                    Secret = secretString
                }
            };
        }
    }
}

