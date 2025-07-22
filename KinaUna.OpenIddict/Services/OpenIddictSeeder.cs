using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.OpenIddict.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace KinaUna.OpenIddict.Services
{
    /// <summary>
    /// Initializes and seeds the OpenIddict database with predefined scopes and client applications.
    /// </summary>
    /// <remarks>This class implements the <see cref="IHostedService"/> interface to perform seeding
    /// operations when the application starts. It ensures that the necessary scopes and client applications are created
    /// in the OpenIddict database if they do not already exist.</remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OpenIddictSeeder"/> class.
    /// </remarks>
    /// <param name="serviceProvider">The service provider used to create scopes and resolve dependencies.</param>
    
    public class OpenIddictSeeder(IServiceProvider serviceProvider) : IHostedService
    {
        /// <summary>
        /// Asynchronously starts the application by ensuring the database is created and initializing OpenIddict scopes
        /// and clients.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Apply pending migrations to ensure the database schema is up-to-date
            await context.Database.MigrateAsync(cancellationToken);
            
            // Perform seeding
            await SeedAsync(scope.ServiceProvider, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to stop the service gracefully.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A completed task since no stop actions are required.</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        
        private async Task SeedAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
        {
            // Todo: Add flag to check if database should be reset.

            // Reset OpenIddict database to ensure a clean state
            await ResetOpenIdDictDatabase(provider);

            // Create scopes first
            await CreateScopesAsync(provider, cancellationToken);

            // Create API clients
            await CreateApiClientsAsync(provider, cancellationToken);
            // Then create web clients
            await CreateWebClientsAsync(provider, cancellationToken);
        }

        private async Task CreateScopesAsync(IServiceProvider provider, CancellationToken cancellationToken)
        {
            // API scopes
            var apiScopes = new[]
            {
                new { Name = Constants.ProgenyApiName, DisplayName = "KinaUna Progeny API" },
                new { Name = Constants.ProgenyApiName + "local", DisplayName = "KinaUna Progeny API Local" },
                new { Name = Constants.ProgenyApiName + "azure", DisplayName = "KinaUna Progeny API Azure" }
            };

            
            IOpenIddictScopeManager scopeManager = provider.GetRequiredService<IOpenIddictScopeManager>();

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

        private async Task CreateApiClientsAsync(IServiceProvider provider, CancellationToken cancellationToken)
        {
            IOpenIddictApplicationManager applicationManager = provider.GetRequiredService<IOpenIddictApplicationManager>();
            // Create Progeny API clients
            IEnumerable<OpenIddictApplicationDescriptor> apiClients = provider.GetRequiredService<IClientConfigProvider>().GetApiClientConfigs();
            foreach (OpenIddictApplicationDescriptor clientConfig in apiClients)
            {
                if (clientConfig.ClientId != null && await applicationManager.FindByClientIdAsync(clientConfig.ClientId, cancellationToken) == null)
                {
                    await applicationManager.CreateAsync(clientConfig, cancellationToken);
                }
            }
        }


        private async Task CreateWebClientsAsync(IServiceProvider provider, CancellationToken cancellationToken)
        {
            IOpenIddictApplicationManager applicationManager = provider.GetRequiredService<IOpenIddictApplicationManager>();
            IClientConfigProvider clientConfigProvider = provider.GetRequiredService<IClientConfigProvider>();

            IEnumerable<OpenIddictApplicationDescriptor> webClients = clientConfigProvider.GetWebClientConfigs();

            foreach (OpenIddictApplicationDescriptor clientConfig in webClients)
            {
                if (clientConfig.ClientId != null && await applicationManager.FindByClientIdAsync(clientConfig.ClientId, cancellationToken) == null)
                {
                    await applicationManager.CreateAsync(clientConfig, cancellationToken);
                }
            }
        }

        private async Task ResetOpenIdDictDatabase(IServiceProvider provider)
        {
            // Clear existing applications
            IOpenIddictApplicationManager openIddictApplicationManager = provider.GetRequiredService<IOpenIddictApplicationManager>();
            await foreach (object application in openIddictApplicationManager.ListAsync())
            {
                await openIddictApplicationManager.DeleteAsync(application);
            }

            // Clear Authorizations
            IOpenIddictAuthorizationManager openIddictAuthorizationManager = provider.GetRequiredService<IOpenIddictAuthorizationManager>();
            await foreach (object authorization in openIddictAuthorizationManager.ListAsync())
            {
                await openIddictAuthorizationManager.DeleteAsync(authorization);
            }

            // Clear Scopes
            IOpenIddictScopeManager openIddictScopeManager = provider.GetRequiredService<IOpenIddictScopeManager>();
            await foreach (object scope in openIddictScopeManager.ListAsync())
            {
                await openIddictScopeManager.DeleteAsync(scope);
            }

            // Clear Tokens
            IOpenIddictTokenManager openIddictTokenManager = provider.GetRequiredService<IOpenIddictTokenManager>();
            await foreach (object token in openIddictTokenManager.ListAsync())
            {
                await openIddictTokenManager.DeleteAsync(token);
            }
        }
    }
}
