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
        /// "SecretString".</exception>
        public IEnumerable<OpenIddictApplicationDescriptor> GetWebClientConfigs()
        {
            string authorityServerUrl = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey) ??
                                        throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + " was not found in the configuration data.");
            string authorityServerUrlLocal = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Local") ??
                                        throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Local was not found in the configuration data.");
            string authorityServerUrlAzure = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Azure") ??
                                        throw new InvalidOperationException("AuthenticationServerAzure was not found in the configuration data.");

            string authorityServerClientId = configuration.GetValue<string>(AuthConstants.AuthenticationServerClientIdKey) ??
                                        throw new InvalidOperationException(AuthConstants.AuthenticationServerClientIdKey + " was not found in the configuration data.");
            string authorityServerClientIdLocal = configuration.GetValue<string>(AuthConstants.AuthenticationServerClientIdKey + "Local") ??
                                        throw new InvalidOperationException(AuthConstants.AuthenticationServerClientIdKey + "Local was not found in the configuration data.");
            string authorityServerClientIdAzure = configuration.GetValue<string>(AuthConstants.AuthenticationServerClientIdKey + "Azure") ??
                                                  throw new InvalidOperationException(AuthConstants.AuthenticationServerClientIdKey + "Azure was not found in the configuration data.");

            string authorityServerApiClientId = configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey) ??
                                                 throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + " was not found in the configuration data.");
            string authorityServerApiClientIdLocal = configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Local") ??
                                                throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Local was not found in the configuration data.");
            string authorityServerApiClientIdAzure = configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Azure") ??
                                                     throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Azure was not found in the configuration data.");


            string authorityServerClientSecret = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey) ??
                                                 throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + " was not found in the configuration data.");
            string authorityServerClientSecretLocal = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Local") ??
                                                 throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Local was not found in the configuration data.");
            string authorityServerClientSecretAzure = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Azure") ??
                                                      throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Azure was not found in the configuration data.");

            string webServerUrl = configuration.GetValue<string>(AuthConstants.WebServerUrlKey) ??
                                  throw new InvalidOperationException(AuthConstants.WebServerUrlKey + " was not found in configuration data.");
            string webServerUrlLocal = configuration.GetValue<string>(AuthConstants.WebServerUrlKey + "Local") ??
                                  throw new InvalidOperationException(AuthConstants.WebServerUrlKey + "Local was not found in configuration data.");
            string webServerUrlAzure = configuration.GetValue<string>(AuthConstants.WebServerUrlKey + "Azure") ??
                                  throw new InvalidOperationException(AuthConstants.WebServerUrlKey + "Azure was not found in configuration data.");

            string webServerClientId = configuration.GetValue<string>(AuthConstants.WebServerClientIdKey) ??
                                       throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + " was not found in configuration data.");
            string webServerClientIdLocal = configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Local") ??
                                            throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + "Local was not found in configuration data.");
            string webServerClientIdAzure = configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Azure") ??
                                            throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + "Azure was not found in configuration data.");

            string webServerApiClientId = configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey) ??
                                          throw new InvalidOperationException(AuthConstants.WebServerApiClientIdKey + " was not found in configuration data.");
            string webServerApiClientIdLocal = configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey + "Local") ??
                                               throw new InvalidOperationException(AuthConstants.WebServerApiClientIdKey + "Local was not found in configuration data.");
            string webServerApiClientIdAzure = configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey + "Azure") ??
                                               throw new InvalidOperationException(AuthConstants.WebServerApiClientIdKey + "Azure was not found in configuration data.");

            string webServerClientSecret = configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey) ??
                                  throw new InvalidOperationException(AuthConstants.WebServerClientSecretKey +" was not found in configuration data.");
            string webServerClientSecretLocal = configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey + "Local") ??
                                           throw new InvalidOperationException(AuthConstants.WebServerClientSecretKey + "Local was not found in configuration data.");
            string webServerClientSecretAzure = configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey + "Azure") ??
                                                throw new InvalidOperationException(AuthConstants.WebServerClientSecretKey + "Azure was not found in configuration data.");

            return
            [
                new OpenIddictApplicationDescriptor
                {
                    ClientId = authorityServerClientId,
                    DisplayName = "KinaUna Auth Client",
                    ClientSecret = authorityServerClientSecret,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName,
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName
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
                    ClientId = authorityServerApiClientId,
                    DisplayName = "KinaUnaAuthApiClient",
                    ClientSecret = authorityServerClientSecret,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName,
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName
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
                    ClientId = webServerClientId,
                    DisplayName = "KinaUnaWeb",
                    ClientSecret = webServerClientSecret,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName, 
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName
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
                    ClientId = webServerApiClientId,
                    DisplayName = "KinaUnaWebApiClient",
                    ClientSecret = webServerClientSecret,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName,
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName
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
                    ClientId = authorityServerClientIdLocal,
                    DisplayName = "KinaUna Auth Client Local",
                    ClientSecret = authorityServerClientSecretLocal,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName + "local"
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
                    ClientId = authorityServerApiClientIdLocal,
                    DisplayName = "KinaUnaAuthApiClientLocal",
                    ClientSecret = authorityServerClientSecretLocal,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName,
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName
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
                    ClientId = webServerClientIdLocal,
                    DisplayName = "KinaUnaWebLocal",
                    ClientSecret = webServerClientSecretLocal,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName + "local"
                    },
                    RedirectUris = { new Uri($"{webServerUrlLocal}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{webServerUrlLocal}/callback/logout") },
                    
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
                    ClientId = webServerApiClientIdLocal,
                    DisplayName = "KinaUnaWebApiClientLocal",
                    ClientSecret = webServerClientSecretLocal,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName + "local"
                        // Permissions.Prefixes.Scope + Scopes.OfflineAccess
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
                    ClientId = authorityServerClientIdAzure,
                    DisplayName = "KinaUna Auth Client Azure",
                    ClientSecret = authorityServerClientSecretAzure,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName + "local",
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName + "local"
                    },
                    RedirectUris = { new Uri($"{authorityServerUrlAzure}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{authorityServerUrlAzure}/callback/logout") },

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
                    ClientId = authorityServerApiClientIdAzure,
                    DisplayName = "KinaUnaAuthApiClientAzure",
                    ClientSecret = authorityServerClientSecretAzure,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName,
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName
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
                    ClientId = webServerClientIdAzure,
                    DisplayName = "KinaUnaWebAzure",
                    ClientSecret = webServerClientSecretAzure,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName + "azure",
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName + "azure"
                    },
                    RedirectUris = { new Uri($"{webServerUrlAzure}/callback/login") },
                    PostLogoutRedirectUris = { new Uri($"{webServerUrlAzure}/callback/logout") },

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
                    ClientId = webServerApiClientIdAzure,
                    DisplayName = "KinaUnaWebApiClientAzure",
                    ClientSecret = webServerClientSecretAzure,
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
                        Permissions.Prefixes.Scope + AuthConstants.ProgenyApiName + "azure",
                        Permissions.Prefixes.Scope + AuthConstants.AuthApiName + "azure"
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
            string progenyApiClient = configuration.GetValue<string>(AuthConstants.ProgenyApiClientIdKey) 
                                      ?? throw new InvalidOperationException(AuthConstants.ProgenyApiClientIdKey + " was not found in the configuration data.");
            string progenyApiClientLocal = configuration.GetValue<string>(AuthConstants.ProgenyApiClientIdKey + "Local") 
                                           ?? throw new InvalidOperationException(AuthConstants.ProgenyApiClientIdKey + "Local was not found in the configuration data.");
            string progenyApiClientAzure = configuration.GetValue<string>(AuthConstants.ProgenyApiClientIdKey + "Azure") ??
                                            throw new InvalidOperationException(AuthConstants.ProgenyApiClientIdKey + "Azure was not found in the configuration data.");

            string progenyApiClientSecret = configuration.GetValue<string>(AuthConstants.ProgenyApiClientSecretKey) ??
                                            throw new InvalidOperationException(AuthConstants.ProgenyApiClientSecretKey + " was not found in the configuration data.");
            string progenyApiClientSecretLocal = configuration.GetValue<string>(AuthConstants.ProgenyApiClientSecretKey + "Local") ??
                                            throw new InvalidOperationException(AuthConstants.ProgenyApiClientSecretKey + "Local was not found in the configuration data.");
            string progenyApiClientSecretAzure = configuration.GetValue<string>(AuthConstants.ProgenyApiClientSecretKey + "Azure") ??
                                                 throw new InvalidOperationException(AuthConstants.ProgenyApiClientSecretKey + "Azure was not found in the configuration data.");

            string authApiClient = configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey) ??
                                      throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + " was not found in the configuration data.");
            string authApiClientLocal = configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Local") 
                                        ?? throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Local was not found in the configuration data.");
            string authApiClientAzure = configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Azure") 
                                        ?? throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Azure was not found in the configuration data.");

            string authClientSecret = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey) ??
                                      throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + " was not found in the configuration data.");
            string authClientSecretLocal = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Local") ??
                                      throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Local was not found in the configuration data.");
            string authClientSecretAzure = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Azure") ??
                                      throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Azure was not found in the configuration data.");

            return
            [
                new OpenIddictApplicationDescriptor
                {
                    ClientId = authApiClient,
                    ClientSecret = authClientSecret,
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
                    ClientId = authApiClientLocal,
                    ClientSecret = authClientSecretLocal,
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
                    ClientId = authApiClientAzure,
                    ClientSecret = authClientSecretAzure,
                    DisplayName = "KinaUna Auth API Azure",
                    ConsentType = ConsentTypes.Implicit,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    },
                    //BaseUrl = "https://kinaunaauth.azurewebsites.net"
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = progenyApiClient,
                    ClientSecret = progenyApiClientSecret,
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
                    ClientId = progenyApiClientLocal,
                    DisplayName = "KinaUna Progeny API Local",
                    ConsentType = ConsentTypes.Implicit,
                    ClientSecret = progenyApiClientSecretLocal,
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                    }
                    //BaseUrl = "https://localhost:44376",
                },
                new OpenIddictApplicationDescriptor
                {
                    ClientId = progenyApiClientAzure,
                    DisplayName = "KinaUna Progeny API Azure",
                    ConsentType = ConsentTypes.Implicit,
                    ClientSecret = progenyApiClientSecretAzure,
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

