using System.Reflection;
using KinaUna.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.OpenIddict.HostingExtensions
{
    internal static class DatabaseConfiguration
    {
        /// <summary>
        /// Configures the database contexts for dependency injection with specified connection strings.
        /// </summary>
        /// <remarks>This method registers the <see cref="ProgenyDbContext"/>, <see
        /// cref="MediaDbContext"/>, and <see cref="ApplicationDbContext"/> with SQL Server support and connection
        /// resiliency enabled. The connection resiliency is configured to retry failed connections up to 15 times with
        /// a maximum delay of 30 seconds between retries. The <see cref="ApplicationDbContext"/> is also configured to
        /// support OpenIddict.</remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the database contexts are added.</param>
        /// <param name="progenyDefaultConnection">The connection string for the Progeny database context.</param>
        /// <param name="mediaDefaultConnection">The connection string for the Media database context.</param>
        /// <param name="authDefaultConnection">The connection string for the Application database context.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> with the configured database contexts.</returns>
        public static void ConfigureDatabases(this IServiceCollection services, string progenyDefaultConnection, string mediaDefaultConnection, string authDefaultConnection)
        {
            // Register the ProgenyDbContext database context with dependency injection.
            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(progenyDefaultConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            // Register the MediaDbContext database context with dependency injection.
            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(mediaDefaultConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            // Register the ApplicationDbContext database context with dependency injection.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(authDefaultConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
                options.UseOpenIddict(); // Add this line to enable OpenIddict support
            });
        }
    }
}
