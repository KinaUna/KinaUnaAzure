using IdentityServer4;
using IdentityServer4.Models;
using KinaUna.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace KinaUna.IDP
{
    public static class Config
    {
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
                    Scopes = new List<Scope>
                    {
                        new Scope(Constants.ProgenyApiName)
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
                    Scopes = new List<Scope>
                    {
                        new Scope(Constants.MediaApiName)
                    }
                }
            };
        }

        public static IEnumerable<Client> GetClients(IConfiguration configuration)
        {
            var webServerUrl = configuration.GetValue<string>("WebServer");
            var webServerAzureUrl = configuration.GetValue<string>("WebServerAzure");
            var webServerLocal = configuration.GetValue<string>("WebServerLocal");
            var secretString = configuration.GetValue<string>("SecretString");
            List<string> corsList = new List<string>();
            corsList.Add(Constants.WebAppUrl);
            corsList.Add(Constants.AuthAppUrl);
            corsList.Add(Constants.ProgenyApiUrl);
            corsList.Add(Constants.MediaApiUrl);
            corsList.Add("https://" + Constants.AppRootDomain);
            return new List<Client>()
            {
                new Client
                {
                    ClientName = "KinaUnaWeb",
                    ClientId = "kinaunawebclient",
                    ClientUri = webServerUrl,
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
                    ClientName = "KinaUnaWebAzure",
                    ClientId = "kinaunawebclientAzure",
                    ClientUri = webServerAzureUrl,
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
                    AccessTokenLifetime = 2592000,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RefreshTokenUsage = TokenUsage.ReUse
                    
                }
            };

        }
    }
    
}
