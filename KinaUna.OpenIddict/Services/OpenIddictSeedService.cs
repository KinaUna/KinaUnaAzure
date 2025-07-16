using KinaUna.Data;
using KinaUna.OpenIddict.Services.Interfaces;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    public class OpenIddictSeedService(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IClientConfigProvider clientConfigProvider)
        : IOpenIddictSeedService
    {
        private const int AccessTokenLifetimeSeconds = 3600; // 1 hour
        private const int RefreshTokenLifetimeDays = 30;

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            // Create scopes first
            await CreateScopesAsync(cancellationToken);
            
            // Then create clients
            await CreateWebClientsAsync(cancellationToken);
        }

        private async Task CreateScopesAsync(CancellationToken cancellationToken)
        {
            // API scopes
            var apiScopes = new[]
            {
                new { Name = Constants.ProgenyApiName, DisplayName = "KinaUna Progeny API" },
                new { Name = Constants.MediaApiName, DisplayName = "KinaUna Media API" }
            };

            foreach (var apiScope in apiScopes)
            {
                if (await scopeManager.FindByNameAsync(apiScope.Name, cancellationToken) == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = apiScope.Name,
                        DisplayName = apiScope.DisplayName,
                        Resources = { apiScope.Name }
                    }, cancellationToken);
                }
            }
        }

        private async Task CreateWebClientsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<ClientConfig> webClients = clientConfigProvider.GetClientConfigs();

            foreach (ClientConfig clientConfig in webClients)
            {
                if (await applicationManager.FindByClientIdAsync(clientConfig.ClientId, cancellationToken) == null)
                {
                    await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = clientConfig.ClientId,
                        ClientSecret = clientConfig.Secret,
                        DisplayName = clientConfig.DisplayName,
                        ConsentType = clientConfig.ConsentType,
                        // Grant types
                        Permissions =
                        {
                            Permissions.Endpoints.Authorization,
                            Permissions.Endpoints.Token,
                            Permissions.Endpoints.EndSession,

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
                            Permissions.Prefixes.Scope + Constants.MediaApiName
                        },

                        RedirectUris = { new Uri($"{clientConfig.BaseUrl}/callback/login/local") },
                        PostLogoutRedirectUris = { new Uri($"{clientConfig.BaseUrl}/callback/logout/local") },

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
                    }, cancellationToken);
                }
            }
        }
    }
}