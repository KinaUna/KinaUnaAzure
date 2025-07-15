using KinaUna.Data;
using KinaUna.Data.Contexts;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Services
{
    /// <summary>
    /// Initializes and seeds the OpenIddict database with predefined scopes and client applications.
    /// </summary>
    /// <remarks>This class implements the <see cref="IHostedService"/> interface to perform seeding
    /// operations when the application starts. It ensures that the necessary scopes and client applications are created
    /// in the OpenIddict database if they do not already exist.</remarks>
    /// <param name="serviceProvider"></param>
    /// <param name="configuration"></param>
    public class OpenIddictSeeder(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
    {
        /// <summary>
        /// Asynchronously starts the application by ensuring the database is created and initializing OpenIddict scopes
        /// and clients.
        /// </summary>
        /// <remarks>This method creates an asynchronous service scope to initialize the database and
        /// configure OpenIddict scopes and clients. It first ensures that the database is created, then creates the
        /// necessary scopes, followed by the creation of web clients.</remarks>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will be canceled if the token is triggered.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Asynchronously creates API scopes using the specified scope manager if they do not already exist.
        /// </summary>
        /// <remarks>This method checks for the existence of predefined API scopes and creates them if
        /// they are not found. The scopes are defined with specific names and display names, and are associated with
        /// their respective resources.</remarks>
        /// <param name="manager">The <see cref="IOpenIddictScopeManager"/> used to manage the creation and retrieval of scopes.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Asynchronously creates web client applications in the OpenIddict application manager if they do not already
        /// exist.
        /// </summary>
        /// <remarks>This method retrieves configuration values for different web server environments and
        /// creates corresponding client applications with predefined settings, including client ID, display name,
        /// consent type, and secret. It ensures that each client application is only created if it does not already
        /// exist in the manager.</remarks>
        /// <param name="manager">The <see cref="IOpenIddictApplicationManager"/> used to manage the OpenIddict applications.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if any required configuration value (e.g., "WebServer", "WebServerAzure", "WebServerLocal",
        /// "OpenIddictSecretString") is not found.</exception>
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
        
        /// <summary>
        /// Initiates an asynchronous operation to stop the service gracefully.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation should be canceled if the token is triggered.</param>
        /// <returns>A task that represents the asynchronous stop operation. The task is completed when the stop operation is
        /// finished.</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Represents the configuration settings for a client application.
        /// </summary>
        /// <remarks>This class is used to store essential information required for client identification
        /// and interaction with a service. It includes properties for client identification, display name, base URL,
        /// secret, and consent type.</remarks>
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
