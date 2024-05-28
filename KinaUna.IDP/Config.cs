using IdentityServer4;
using IdentityServer4.Models;
using KinaUna.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using IdentityModel;

namespace KinaUna.IDP
{
    /// <summary>
    /// Configuration for the Identity Server.
    /// </summary>
    public static class Config
    {
        // If you make changes here, temporarily set ResetIdentityDb (KinaUna.Data/Constants.cs) to true to reset the database.

        // identity-related resources (scopes)
        public static List<IdentityResource> GetIdentityResources()
        {
            return
            [
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResource(
                    "firstname",
                    "Your First Name",
                    ["firstname"]),
                new IdentityResource(
                    "middlename",
                    "Your Middle Name",
                    ["middlename"]),
                new IdentityResource(
                    "lastname",
                    "Your Last Name",
                    ["lastname"]),
                new IdentityResource(
                    "roles",
                    "Your role(s)",
                    ["role"]),
                new IdentityResource(
                    "timezone",
                    "Your timezone",
                    ["timezone"]),
                new IdentityResource(
                    "viewchild",
                    "Currently Viewing Child",
                    ["viewchild"]),
                new IdentityResource(
                    "joindate",
                    "Joined Date",
                    ["joindate"])
            ];
        }

        // API related resources (Scopes)
        public static List<ApiResource> GetApiResources(IConfiguration configuration)
        {
            string secretString = configuration.GetValue<string>("SecretString");
            return
            [
                new ApiResource{
                    Name = Constants.ProgenyApiName,
                    DisplayName = "KinaUna Progeny API",
                    UserClaims = [JwtClaimTypes.Role, JwtClaimTypes.Subject, JwtClaimTypes.Email, "timezone"],
                    ApiSecrets =
                    [
                        new Secret(secretString.Sha256())
                    ],
                    Scopes =
                    [
                       Constants.ProgenyApiName, Constants.MediaApiName
                    ]
                },
                new ApiResource{
                    Name = Constants.MediaApiName,
                    DisplayName = "KinaUna Media API",
                    UserClaims = [JwtClaimTypes.Role, JwtClaimTypes.Subject, JwtClaimTypes.Email, "timezone"],
                    ApiSecrets =
                    [
                        new Secret(secretString.Sha256())
                    ],
                    Scopes =
                    [
                        Constants.ProgenyApiName, Constants.MediaApiName
                    ]
                }
            ];
        }

        public static List<ApiScope> ApiScopes =>
            [
                new ApiScope(Constants.ProgenyApiName, "KinaUna Progeny API",["role"]),
                new ApiScope(Constants.MediaApiName, "KinaUna Media API",["role"])
            ];

        public static List<Client> GetClients(IConfiguration configuration)
        {
            string webServerUrl = configuration.GetValue<string>("WebServer");
            string webBlazorServerUrl = configuration.GetValue<string>("WebBlazorServer");
            string webServerAzureUrl = configuration.GetValue<string>("WebServerAzure");
            string webServerLocal = configuration.GetValue<string>("WebServerLocal");
            string webBlazorServerLocal = configuration.GetValue<string>("WebBlazorServerLocal");
            string secretString = configuration.GetValue<string>("SecretString");
            List<string> corsList =
            [
                Constants.WebAppUrl,
                Constants.AuthAppUrl,
                Constants.ProgenyApiUrl,
                Constants.MediaApiUrl,
                "https://" + Constants.AppRootDomain,
            ];
            return
            [
                new Client
                {
                    ClientName = "KinaUnaWebBlazor",
                    ClientId = "kinaunawebblazorclient",
                    ClientUri = webBlazorServerUrl,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris =
                    [
                        webServerUrl + "/signin-oidc"

                    ],
                    PostLogoutRedirectUris =
                    [
                        webServerUrl + "/signout-callback-oidc"
                    ],
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Email,
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
                    RedirectUris =
                    [
                        webServerUrl + "/signin-oidc"
                        
                    ],
                    PostLogoutRedirectUris =
                    [
                        webServerUrl + "/signout-callback-oidc"
                    ],
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Email,
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
                    ClientName = "KinaUnaWebBlazorLocal",
                    ClientId = "kinaunawebblazorclientlocal",
                    ClientUri = webBlazorServerLocal,
                    RequirePkce = false,
                    AllowPlainTextPkce = false,
                    RequireConsent = false,
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris =
                    [
                        webBlazorServerLocal + "/signin-oidc"

                    ],
                    PostLogoutRedirectUris =
                    [
                        webBlazorServerLocal + "/signout-callback-oidc"
                    ],
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Email,
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
                    RedirectUris =
                    [
                        webServerLocal + "/signin-oidc"
                        
                    ],
                    PostLogoutRedirectUris =
                    [
                        webServerLocal + "/signout-callback-oidc"
                    ],
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Email,
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
                    RedirectUris =
                    [
                        webServerAzureUrl + "/signin-oidc"

                    ],
                    PostLogoutRedirectUris =
                    [
                        webServerAzureUrl + "/signout-callback-oidc"
                    ],
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        IdentityServerConstants.StandardScopes.Email,
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
                    ClientName = "KinaUnaMaui",
                    ClientId = "kinaunamaui",
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = { "kinaunamaui://callback" },
                    PostLogoutRedirectUris = { "kinaunamaui://callback" },
                    RequireClientSecret = false,
                    RequireConsent = false,
                    RequirePkce = true,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        Constants.ProgenyApiName,
                        Constants.MediaApiName
                    },
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RefreshTokenUsage = TokenUsage.ReUse
                }
            ];

        }
    }
}
