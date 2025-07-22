using KinaUna.Data;
using KinaUna.OpenIddict.Services.Interfaces;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    public class ConfigurationClientConfigProvider(IConfiguration configuration) : IClientConfigProvider
    {
        private const int AccessTokenLifetimeSeconds = 3600; // 1 hour
        private const int RefreshTokenLifetimeDays = 30;

        public IEnumerable<OpenIddictApplicationDescriptor> GetWebClientConfigs()
        {
            string webServerUrl = configuration.GetValue<string>("WebServer") ??
                                  throw new InvalidOperationException("WebServer not found in configuration data.");
            // string webServerAzureUrl = configuration.GetValue<string>("WebServerAzure") ??
            //                           throw new InvalidOperationException("WebServerAzure not found in configuration data.");
            string webServerLocal = configuration.GetValue<string>("WebServerLocal") ??
                                    throw new InvalidOperationException("WebServerLocal not found in configuration data.");
            string secretString = configuration.GetValue<string>("OpenIddictSecretString") ??
                                  throw new InvalidOperationException("OpenIddictSecretString not found in configuration data.");
            string secretStringLocal = configuration.GetValue<string>("OpenIddictSecretStringLocal") ??
                                  throw new InvalidOperationException("OpenIddictSecretStringLocal not found in configuration data.");

            return new[]
            {
                new OpenIddictApplicationDescriptor
                {
                    ClientId = "kinaunawebclient",
                    DisplayName = "KinaUnaWeb",
                    ClientSecret = secretString,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,

                        // Scopes
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Roles,
                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName,
                        Permissions.Prefixes.Scope + Scopes.OfflineAccess
                    },
                    RedirectUris = { new Uri($"{webServerUrl}/callback/login/local") },
                    PostLogoutRedirectUris = { new Uri($"{webServerUrl}/callback/logout/local") },

                    Requirements =
                    {
                        Requirements.Features.ProofKeyForCodeExchange
                    },

                    // Token settings
                    Settings =
                    {
                        [Settings.TokenLifetimes.AccessToken] = TimeSpan.FromSeconds(AccessTokenLifetimeSeconds).ToString(),
                        [Settings.TokenLifetimes.RefreshToken] = TimeSpan.FromDays(RefreshTokenLifetimeDays).ToString()
                    }
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = "kinaunawebapiclient",
                    DisplayName = "KinaUnaWebApiClient",
                    ClientSecret = secretString,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.RefreshToken,
                        
                        // Scopes
                        Permissions.Scopes.Roles,

                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName,
                        Permissions.Prefixes.Scope + Scopes.OfflineAccess
                    }
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = "kinaunawebclientlocal",
                    DisplayName = "KinaUnaWebLocal",
                    ClientSecret = secretStringLocal,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,
                        
                        // Scopes
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Roles,

                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + Scopes.OfflineAccess
                    },
                    RedirectUris = { new Uri($"{webServerLocal}/callback/login/local") },
                    PostLogoutRedirectUris = { new Uri($"{webServerLocal}/callback/logout/local") },
                    
                    Requirements =
                    {
                        Requirements.Features.ProofKeyForCodeExchange
                    }
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = "kinaunawebapiclientlocal",
                    DisplayName = "KinaUnaWebApiClientLocal",
                    ClientSecret = secretStringLocal,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.RefreshToken,

                        // Scopes
                        Permissions.Scopes.Roles,
                        
                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + Scopes.OfflineAccess
                    }
                }
            };
        }

        public IEnumerable<OpenIddictApplicationDescriptor> GetApiClientConfigs()
        {
            string secretString = configuration.GetValue<string>("OpenIddictSecretString") ??
                                  throw new InvalidOperationException("OpenIddictSecretString not found in configuration data.");
            string secretStringLocal = configuration.GetValue<string>("OpenIddictSecretStringLocal") ??
                                  throw new InvalidOperationException("OpenIddictSecretStringLocal not found in configuration data.");
            return new[]
            {
                new OpenIddictApplicationDescriptor
                {
                    ClientId = Constants.ProgenyApiName,
                    ClientSecret = secretString,
                    DisplayName = "KinaUna Progeny API",
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    },
                    //BaseUrl = "https://progenyapi.kinauna.com"
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = Constants.ProgenyApiName + "local",
                    DisplayName = "KinaUna Progeny API Local",
                    ConsentType = ConsentTypes.Implicit,
                    ClientSecret = secretStringLocal,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    }
                    //BaseUrl = "https://localhost:44376",
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = Constants.ProgenyApiName + "azure",
                    DisplayName = "KinaUna Progeny API Azure",
                    ConsentType = ConsentTypes.Implicit,
                    ClientSecret = secretString,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    }
                    // BaseUrl = "https://kinaunaprogenyapi.azurewebsites.net",
                }
            };
        }
    }
}

