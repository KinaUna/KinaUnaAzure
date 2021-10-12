using IdentityServer4;
using IdentityServer4.Models;
using KinaUna.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace KinaUna.IDP
{
    /// <summary>
    /// Configuration for the Identity Server.
    /// </summary>
    public static class Config
    {
        // If you make changes here, temporarily set ResetIdentityDb (KinaUna.Data/Constants.cs) to true to reset the database.

        // identity-related resources (scopes)
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResource(
                    "firstname",
                    "Your First Name",
                    new List<string>(){"firstname"}),
                new IdentityResource(
                    "middlename",
                    "Your Middle Name",
                    new List<string>(){"middlename"}),
                new IdentityResource(
                    "lastname",
                    "Your Last Name",
                    new List<string>(){"lastname"}),
                new IdentityResource(
                    "roles",
                    "Your role(s)",
                    new List<string>(){"role"}),
                new IdentityResource(
                    "timezone",
                    "Your timezone",
                    new List<string>(){"timezone"}),
                new IdentityResource(
                    "viewchild",
                    "Currently Viewing Child",
                    new List<string>(){"viewchild"}),
                new IdentityResource(
                    "joindate",
                    "Joined Date",
                    new List<string>(){"joindate"})
            };
        }

        // API related resources (Scopes)
        public static IEnumerable<ApiResource> GetApiResources(IConfiguration configuration)
        {
            var secretString = configuration.GetValue<string>("SecretString");
            return new List<ApiResource>
            {
                new ApiResource{
                    Name = Constants.ProgenyApiName,
                    DisplayName = "KinaUna Progeny API",
                    UserClaims = new List<string> {"role" },
                    ApiSecrets = new List<Secret>
                    {
                        new Secret(secretString.Sha256())
                    },
                    Scopes = new List<string>
                    {
                       Constants.ProgenyApiName
                    }
                },
                new ApiResource{
                    Name = Constants.MediaApiName,
                    DisplayName = "KinaUna Media API",
                    UserClaims = new List<string> {"role" },
                    ApiSecrets = new List<Secret>
                    {
                        new Secret(secretString.Sha256())
                    },
                    Scopes = new List<string>
                    {
                        Constants.ProgenyApiName
                    }
                },
                new ApiResource{
                    Name = Constants.PivoqApiName,
                    DisplayName = "Pivoq API",
                    UserClaims = new List<string> {"role" },
                    ApiSecrets = new List<Secret>
                    {
                        new Secret(secretString.Sha256())
                    },
                    Scopes = new List<string>
                    {
                        Constants.PivoqApiName
                    }
                },
                new ApiResource
                {
                    Name = Constants.PivoqCoreApiName,
                    DisplayName = "Pivoq Core API",
                    UserClaims = new List<string> { "role" },
                    ApiSecrets = new List<Secret>
                    {
                        new Secret(secretString.Sha256())
                    },
                    Scopes = new List<string>
                    {
                        Constants.PivoqApiName
                    }
                }
            };
        }

        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope(Constants.ProgenyApiName, "KinaUna Progeny API",new List<string> {"role" }),
                new ApiScope(Constants.MediaApiName, "KinaUna Media API",new List<string> {"role" }),
                new ApiScope(Constants.PivoqApiName, "Pivoq API",new List<string> {"role" }),
                new ApiScope(Constants.PivoqCoreApiName, "Pivoq Core API", new List<string> { "role" })

            };

        public static IEnumerable<Client> GetClients(IConfiguration configuration)
        {
            var webServerUrl = configuration.GetValue<string>("WebServer");
            var webServerAzureUrl = configuration.GetValue<string>("WebServerAzure");
            var supportServerUrl = configuration.GetValue<string>("SupportServer");
            var webServerLocal = configuration.GetValue<string>("WebServerLocal");
            var webServerNuuk2015 = configuration.GetValue<string>("WebServerDevNuuk2015");
            var webServerNuuk2020 = configuration.GetValue<string>("WebServerDevNuuk2020");
            var pivoqWebServerUrl = configuration.GetValue<string>("PivoqWebServer");
            var pivoqCoreWebServerUrl = configuration.GetValue<string>("PivoqCoreWebServer");
            var pivoqWebServerLocal = configuration.GetValue<string>("PivoqWebServerLocal");
            var pivoqWebServerNuuk2015 = configuration.GetValue<string>("PivoqWebServerDevNuuk2015");
            var pivoqWebServerNuuk2020 = configuration.GetValue<string>("PivoqWebServerDevNuuk2020");
            var secretString = configuration.GetValue<string>("SecretString");
            List<string> corsList = new List<string>();
            corsList.Add(Constants.WebAppUrl);
            corsList.Add(Constants.AuthAppUrl);
            corsList.Add(Constants.ProgenyApiUrl);
            corsList.Add(Constants.MediaApiUrl);
            corsList.Add(Constants.SupportUrl);
            corsList.Add(Constants.PivoqUrl);
            corsList.Add(Constants.PivoqApiUrl);
            corsList.Add(Constants.PivoqCoreUrl);
            corsList.Add(Constants.PivoqCoreApiUrl);
            corsList.Add("https://" + Constants.AppRootDomain);
            corsList.Add(webServerNuuk2015);
            corsList.Add("https://nuuk2015.kinauna.io");
            corsList.Add(webServerNuuk2020);
            corsList.Add("https://nuuk2020.kinauna.io");
            //corsList.Add("https://*.kinauna.io");
            corsList.Add(pivoqWebServerNuuk2015);
            corsList.Add(pivoqWebServerNuuk2020);
            return new List<Client>()
            {
                new Client
                {
                    ClientName = "KinaUnaWeb",
                    ClientId = "kinaunawebclient",
                    ClientUri = webServerUrl,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    IdentityTokenLifetime = 2592000,
                    AuthorizationCodeLifetime = 2592000,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowedCorsOrigins = corsList,
                    RedirectUris = new List<string>()
                    {
                        webServerUrl + "/signin-oidc"
                        
                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        webServerUrl + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "KinaUnaWebLocal",
                    ClientId = "kinaunawebclientlocal",
                    ClientUri = webServerLocal,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = new List<string>()
                    {
                        webServerLocal + "/signin-oidc"
                        
                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        webServerLocal + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "KinaUnaWebNuuk2015",
                    ClientId = "kinaunawebclientnuuk2015",
                    ClientUri = webServerNuuk2015,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = new List<string>()
                    {
                        webServerNuuk2015 + "/signin-oidc"
                        
                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        webServerNuuk2015 + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "KinaUnaWebNuuk2020",
                    ClientId = "kinaunawebclientnuuk2020",
                    ClientUri = webServerNuuk2020,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = new List<string>()
                    {
                        webServerNuuk2020 + "/signin-oidc"
                        
                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        webServerNuuk2020 + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "KinaUnaWebAzure",
                    ClientId = "kinaunawebclientAzure",
                    ClientUri = webServerAzureUrl,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    IdentityTokenLifetime = 2592000,
                    AuthorizationCodeLifetime = 2592000,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    //AbsoluteRefreshTokenLifetime = ...
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowedCorsOrigins = corsList,
                    RedirectUris = new List<string>()
                    {
                        webServerAzureUrl + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        webServerAzureUrl + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "KinaUnaXamarin",
                    ClientId = "kinaunaxamarin",
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    RedirectUris = {"kinaunaxamarinclients://callback"},
                    PostLogoutRedirectUris = {"kinaunaxamarinclients://callback"},
                    RequireClientSecret = false,
                    RequireConsent = false,
                    RequirePkce = true,
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RefreshTokenUsage = TokenUsage.ReUse
                    
                },
                new Client
                {
                    ClientName = "KinaUnaXamarin2",
                    ClientId = "kinaunaxamarin2",
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = {"kinaunaxamarinclients://callback"},
                    PostLogoutRedirectUris = {"kinaunaxamarinclients://callback"},
                    RequireClientSecret = false,
                    RequireConsent = false,
                    RequirePkce = true,
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RefreshTokenUsage = TokenUsage.ReUse

                },
                new Client
                {
                    ClientName = "KinaUnaXamarin3",
                    ClientId = "kinaunaxamarin3",
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = { "net.kinauna.xamarin://callback" },
                    PostLogoutRedirectUris = { "net.kinauna.xamarin://callback" },
                    RequireClientSecret = false,
                    RequireConsent = false,
                    RequirePkce = true,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RefreshTokenUsage = TokenUsage.ReUse

                },
                new Client
                {
                    ClientName = "KinaUnaSupport",
                    ClientId = "kinaunasupport",
                    ClientUri = supportServerUrl,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    IdentityTokenLifetime = 2592000,
                    AuthorizationCodeLifetime = 2592000,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    //AbsoluteRefreshTokenLifetime = ...
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowedCorsOrigins = corsList,
                    RedirectUris = new List<string>()
                    {
                        supportServerUrl + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        supportServerUrl + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate"
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "pivoqcoreweb",
                    ClientId = "pivoqcorewebclient",
                    ClientUri = pivoqCoreWebServerUrl,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    IdentityTokenLifetime = 2592000,
                    AuthorizationCodeLifetime = 2592000,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowedCorsOrigins = corsList,
                    RedirectUris = new List<string>()
                    {
                        pivoqCoreWebServerUrl + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        pivoqCoreWebServerUrl + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName,
                        Constants.PivoqApiName,
                        Constants.PivoqCoreApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "pivoqweb",
                    ClientId = "pivoqwebclient",
                    ClientUri = pivoqWebServerUrl,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    IdentityTokenLifetime = 2592000,
                    AuthorizationCodeLifetime = 2592000,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowedCorsOrigins = corsList,
                    RedirectUris = new List<string>()
                    {
                        pivoqWebServerUrl + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        pivoqWebServerUrl + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName,
                        Constants.PivoqApiName,
                        Constants.PivoqCoreApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "PivoqWebLocal",
                    ClientId = "pivoqwebclientlocal",
                    ClientUri = pivoqWebServerLocal,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = new List<string>()
                    {
                        pivoqWebServerLocal + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        pivoqWebServerLocal + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName,
                        Constants.PivoqApiName,
                        Constants.PivoqCoreApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "PivoqWebNuuk2015",
                    ClientId = "pivoqwebclientnuuk2015",
                    ClientUri = pivoqWebServerNuuk2015,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = new List<string>()
                    {
                        pivoqWebServerNuuk2015 + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        pivoqWebServerNuuk2015 + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName,
                        Constants.PivoqApiName,
                        Constants.PivoqCoreApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                },
                new Client
                {
                    ClientName = "PivoqWebNuuk2020",
                    ClientId = "pivoqwebclientnuuk2020",
                    ClientUri = pivoqWebServerNuuk2020,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = new List<string>()
                    {
                        pivoqWebServerNuuk2020 + "/signin-oidc"

                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        pivoqWebServerNuuk2020 + "/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "firstname",
                        "middlename",
                        "lastname",
                        "roles",
                        "timezone",
                        "viewchild",
                        "joindate",
                        Constants.ProgenyApiName,
                        Constants.MediaApiName,
                        Constants.PivoqApiName,
                        Constants.PivoqCoreApiName
                    },
                    ClientSecrets =
                    {
                        new Secret(secretString.Sha256())
                    }
                }
            };

        }
    }
    
}
