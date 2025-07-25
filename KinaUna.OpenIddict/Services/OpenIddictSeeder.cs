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
    
    public class OpenIddictSeeder(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
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
            await SeedAsync(scope.ServiceProvider, configuration, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to stop the service gracefully.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A completed task since no stop actions are required.</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        
        private static async Task SeedAsync(IServiceProvider provider, IConfiguration configuration, CancellationToken cancellationToken = default)
        {
            bool resetDatabase = false;
            // Check if the database should be reset
            // This can be controlled by an environment variable or configuration setting
            try
            {
                bool? resetDatabaseEnv = configuration.GetValue<bool>("ResetOpenIddictDatabase");
                if (resetDatabaseEnv.HasValue)
                {

                    resetDatabase = resetDatabaseEnv.Value;
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                Console.WriteLine($"Error checking environment variable for resetting OpenIddict database: {ex.Message}");
            }
            if (resetDatabase)
            {
                // Uncomment the line below to reset the OpenIddict database
                await ResetOpenIdDictDatabase(provider);
            }
            // Create scopes first
            await CreateScopesAsync(provider, cancellationToken);

            // Create API clients
            await CreateApiClientsAsync(provider, cancellationToken);
            // Then create web clients
            await CreateWebClientsAsync(provider, cancellationToken);
        }

        /// <summary>
        /// Asynchronously creates API scopes if they do not already exist in the OpenIddict scope manager.
        /// </summary>
        /// <remarks>This method checks for the existence of predefined API scopes and creates them if
        /// they are not found. It uses the <see cref="IOpenIddictScopeManager"/> to manage the scopes, ensuring that
        /// each scope is uniquely identified by its name.</remarks>
        /// <param name="provider">The service provider used to resolve the <see cref="IOpenIddictScopeManager"/> service.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns></returns>
        private static async Task CreateScopesAsync(IServiceProvider provider, CancellationToken cancellationToken)
        {
            // API scopes
            var apiScopes = new[]
            {
                new { Name = Constants.ProgenyApiName, DisplayName = "KinaUna Progeny API" },
                new { Name = Constants.ProgenyApiName + "local", DisplayName = "KinaUna Progeny API Local" },
                new { Name = Constants.ProgenyApiName + "azure", DisplayName = "KinaUna Progeny API Azure" },
                new { Name = Constants.AuthApiName, DisplayName = "KinaUna Auth API" },
                new { Name = Constants.AuthApiName + "local", DisplayName = "KinaUna Auth API Local" },
                new { Name = Constants.AuthApiName + "azure", DisplayName = "KinaUna Auth API Azure" }
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

        /// <summary>
        /// Asynchronously creates API clients using the provided service provider and cancellation token.
        /// </summary>
        /// <remarks>This method retrieves API client configurations and creates new clients if they do
        /// not already exist.</remarks>
        /// <param name="provider">The service provider used to resolve dependencies required for creating API clients.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns></returns>
        private static async Task CreateApiClientsAsync(IServiceProvider provider, CancellationToken cancellationToken)
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

        /// <summary>
        /// Asynchronously creates web client applications based on the provided configurations.
        /// </summary>
        /// <remarks>This method retrieves web client configurations and creates new client applications
        /// if they do not already exist in the application manager.</remarks>
        /// <param name="provider">The service provider used to resolve required services.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns></returns>
        private static async Task CreateWebClientsAsync(IServiceProvider provider, CancellationToken cancellationToken)
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

        /// <summary>
        /// Resets the OpenIddict database by removing all existing applications, authorizations, scopes, and tokens. User data is not affected.
        /// </summary>
        /// <remarks>This method clears all entries in the OpenIddict database, effectively resetting its
        /// state. It is intended for scenarios where a complete reset of the OpenIddict data is required.</remarks>
        /// <param name="provider">The service provider used to resolve the OpenIddict managers.</param>
        /// <returns></returns>
        private static async Task ResetOpenIdDictDatabase(IServiceProvider provider)
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

            // Clear Authorization entries
            IOpenIddictAuthorizationManager openIddictAuthorizationEntryManager = provider.GetRequiredService<IOpenIddictAuthorizationManager>();
            await foreach (object authorizationEntry in openIddictAuthorizationEntryManager.ListAsync())
            {
                await openIddictAuthorizationEntryManager.DeleteAsync(authorizationEntry);
            }
        }
    }
}
