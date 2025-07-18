using KinaUna.Data.Contexts;
using KinaUna.OpenIddict.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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
    /// <param name="seedService">The service that performs the actual seeding operations.</param>
    public class OpenIddictSeeder(IServiceProvider serviceProvider, IOpenIddictSeedService seedService) : IHostedService
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
            await seedService.SeedAsync(cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to stop the service gracefully.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A completed task since no stop actions are required.</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
