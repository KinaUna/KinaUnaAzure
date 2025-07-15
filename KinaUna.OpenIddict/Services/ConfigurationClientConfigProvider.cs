using KinaUna.OpenIddict.Services.Interfaces;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    public class ConfigurationClientConfigProvider : IClientConfigProvider
    {
        private readonly IConfiguration _configuration;

        public ConfigurationClientConfigProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<ClientConfig> GetClientConfigs()
        {
            string webServerUrl = _configuration.GetValue<string>("WebServer") ?? 
                throw new InvalidOperationException("WebServer not found in configuration data.");
            string webServerAzureUrl = _configuration.GetValue<string>("WebServerAzure") ?? 
                throw new InvalidOperationException("WebServerAzure not found in configuration data.");
            string webServerLocal = _configuration.GetValue<string>("WebServerLocal") ?? 
                throw new InvalidOperationException("WebServerLocal not found in configuration data.");
            string secretString = _configuration.GetValue<string>("OpenIddictSecretString") ?? 
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

