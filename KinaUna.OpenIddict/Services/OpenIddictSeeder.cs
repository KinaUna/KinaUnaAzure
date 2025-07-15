using KinaUna.Data;
using KinaUna.Data.Contexts;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    public class OpenIddictSeeder(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
    {
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            IOpenIddictApplicationManager manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            IOpenIddictScopeManager scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            // Ensure the database is created
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            // Create scopes first
            await CreateScopesAsync(scopeManager);

            // Then create clients
            await CreateWebClientsAsync(manager);
        }

        private async Task CreateScopesAsync(IOpenIddictScopeManager manager)
        {
            // API scopes
            var apiScopes = new[]
            {
                new { Name = Constants.ProgenyApiName, DisplayName = "KinaUna Progeny API" },
                new { Name = Constants.MediaApiName, DisplayName = "KinaUna Media API" }
            };

            foreach (var apiScope in apiScopes)
            {
                if (await manager.FindByNameAsync(apiScope.Name) == null)
                {
                    await manager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = apiScope.Name,
                        DisplayName = apiScope.DisplayName,
                        Resources = { apiScope.Name }
                    });
                }
            }
        }

        private async Task CreateWebClientsAsync(IOpenIddictApplicationManager manager)
        {
            string webServerUrl = configuration.GetValue<string>("WebServer") ?? throw new InvalidOperationException("WebServer not found in configuration data.");
            string webServerAzureUrl = configuration.GetValue<string>("WebServerAzure") ?? throw new InvalidOperationException("WebServerAzure not found in configuration data.");
            string webServerLocal = configuration.GetValue<string>("WebServerLocal") ?? throw new InvalidOperationException("WebServerLocal not found in configuration data.");
            string secretString = configuration.GetValue<string>("OpenIddictSecretString") ?? throw new InvalidOperationException("OpenIddictSecretString not found in configuration data.");

            ClientConfig[] webClients = new[]
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

            foreach (ClientConfig clientConfig in webClients)
            {
                if (await manager.FindByClientIdAsync(clientConfig.ClientId) == null)
                {
                    await manager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = clientConfig.ClientId,
                        ClientSecret = clientConfig.Secret, // OpenIddict will hash this automatically
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
                            [Settings.TokenLifetimes.AccessToken] = TimeSpan.FromSeconds(300).ToString(),
                            [Settings.TokenLifetimes.RefreshToken] = TimeSpan.FromDays(30).ToString()
                        }
                    });
                }
            }
        }
        
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private class ClientConfig
        {
            public required string ClientId { get; init; }
            public required string DisplayName { get; init; }
            public required string BaseUrl { get; init; }
            public required string Secret { get; set; }
            public required string ConsentType { get; init; }
        }
    }
}
