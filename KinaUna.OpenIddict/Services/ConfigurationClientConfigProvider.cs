using KinaUna.Data;
using KinaUna.OpenIddict.Services.Interfaces;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    /// <summary>
    /// Provides configuration for client applications using OpenIddict.
    /// </summary>
    /// <remarks>This class retrieves client configuration settings from an <see cref="IConfiguration"/>
    /// source and constructs <see cref="OpenIddictApplicationDescriptor"/> instances for web and API clients.</remarks>
    /// <param name="configuration"></param>
    public class ConfigurationClientConfigProvider(IConfiguration configuration) : IClientConfigProvider
    {
        private const int AccessTokenLifetimeSeconds = 3600; // 1 hour
        private const int RefreshTokenLifetimeDays = 30;

        /// <summary>
        /// Retrieves a collection of OpenIddict application configurations for web clients.
        /// </summary>
        /// <remarks>This method constructs and returns a set of <see
        /// cref="OpenIddictApplicationDescriptor"/> objects, each representing a web client configuration with specific
        /// permissions, redirect URIs, and token settings. The configurations are based on values retrieved from the
        /// application's configuration data.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="OpenIddictApplicationDescriptor"/> objects, each containing
        /// the configuration details for a web client.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any required configuration value is missing, such as "AuthenticationServer", "WebServer", or
        /// "OpenIddictSecretString".</exception>
        public IEnumerable<OpenIddictApplicationDescriptor> GetWebClientConfigs()
        {
            string authorityServerUrl = configuration.GetValue<string>("AuthenticationServer") ??
                                        throw new InvalidOperationException("AuthenticationServer was not found in the configuration data.");
            string authorityServerUrlLocal = configuration.GetValue<string>("AuthenticationServerLocal") ??
                                        throw new InvalidOperationException("AuthenticationServerLocal was not found in the configuration data.");
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

            return
            [
                new OpenIddictApplicationDescriptor
                {
                    ClientId = "authclient",
                    DisplayName = "KinaUna Auth Client",
                    ClientSecret = secretString,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.GrantTypes.TokenExchange,
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.EndSession,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,

                        // Scopes
                        Permissions.Scopes.Roles,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,

                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName,
                        Permissions.Prefixes.Scope + Constants.AuthApiName
                    },
                    RedirectUris = { new Uri($"{authorityServerUrl}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{authorityServerUrl}/callback/logout") },

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
                    ClientId = "kinaunawebclient",
                    DisplayName = "KinaUnaWeb",
                    ClientSecret = secretString,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.GrantTypes.TokenExchange,
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.EndSession,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,

                        // Scopes
                        Permissions.Scopes.Roles,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,
                        
                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName, 
                        Permissions.Prefixes.Scope + Constants.AuthApiName
                    },
                    RedirectUris = { new Uri($"{webServerUrl}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{webServerUrl}/callback/logout") },

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
                        Permissions.GrantTypes.TokenExchange,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.RefreshToken,
                        
                        // Scopes
                        Permissions.Scopes.Roles,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,

                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName,
                        Permissions.Prefixes.Scope + Constants.AuthApiName
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
                    ClientId = "authclientlocal",
                    DisplayName = "KinaUna Auth Client Local",
                    ClientSecret = secretString,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.GrantTypes.TokenExchange,
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.EndSession,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,

                        // Scopes
                        Permissions.Scopes.Roles,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,

                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + Constants.AuthApiName + "local"
                    },
                    RedirectUris = { new Uri($"{authorityServerUrlLocal}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{authorityServerUrlLocal}/callback/logout") },

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
                    ClientId = "kinaunawebclientlocal",
                    DisplayName = "KinaUnaWebLocal",
                    ClientSecret = secretStringLocal,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.GrantTypes.TokenExchange,
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.EndSession,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,

                        // Scopes
                        Permissions.Scopes.Roles,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,

                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + Constants.AuthApiName + "local"
                    },
                    RedirectUris = { new Uri($"{webServerLocal}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{webServerLocal}/callback/logout") },
                    
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
                    ClientId = "kinaunawebapiclientlocal",
                    DisplayName = "KinaUnaWebApiClientLocal",
                    ClientSecret = secretStringLocal,
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.GrantTypes.TokenExchange,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.RefreshToken,
                        
                        // Scopes
                        Permissions.Scopes.Roles,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,
                        
                        // Custom scopes
                        Permissions.Prefixes.Scope + Constants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + Constants.AuthApiName + "local"
                        // Permissions.Prefixes.Scope + Scopes.OfflineAccess
                    },
                    // Token settings
                    Settings =
                    {
                        [Settings.TokenLifetimes.AccessToken] = TimeSpan.FromSeconds(AccessTokenLifetimeSeconds).ToString(),
                        [Settings.TokenLifetimes.RefreshToken] = TimeSpan.FromDays(RefreshTokenLifetimeDays).ToString()
                    }
                }
            ];
        }

        /// <summary>
        /// Retrieves a collection of OpenIddict application configurations for API clients.
        /// </summary>
        /// <remarks>This method fetches the client configurations from the application settings,
        /// including client identifiers, secrets, display names, consent types, and permissions. It throws an <see
        /// cref="InvalidOperationException"/> if the required secret strings are not found in the configuration
        /// data.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="OpenIddictApplicationDescriptor"/> objects, each representing
        /// the configuration for an API client.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration data does not contain the required secret strings.</exception>
        public IEnumerable<OpenIddictApplicationDescriptor> GetApiClientConfigs()
        {
            string secretString = configuration.GetValue<string>("OpenIddictSecretString") ??
                                  throw new InvalidOperationException("OpenIddictSecretString not found in configuration data.");
            string secretStringLocal = configuration.GetValue<string>("OpenIddictSecretStringLocal") ??
                                  throw new InvalidOperationException("OpenIddictSecretStringLocal not found in configuration data.");
            return
            [
                new OpenIddictApplicationDescriptor
                {
                    ClientId = Constants.AuthApiName,
                    ClientSecret = secretString,
                    DisplayName = "KinaUna Auth API",
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    },
                    //BaseUrl = "https://auth.kinauna.com"
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = Constants.AuthApiName + "local",
                    ClientSecret = secretString,
                    DisplayName = "KinaUna Auth API Local",
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    },
                    //BaseUrl = "https://localhost:44397"
                },
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
            ];
        }
    }
}

