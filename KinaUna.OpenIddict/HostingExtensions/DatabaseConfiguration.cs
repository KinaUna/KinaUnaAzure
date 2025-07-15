using KinaUna.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.OpenIddict.HostingExtensions
{
    internal static class DatabaseConfiguration
    {
        public static IServiceCollection ConfigureDatabases(this IServiceCollection services, string progenyDefaultConnection, string mediaDefaultConnection, string authDefaultConnection)
        {
            // Register the ProgenyDbContext database context with dependency injection.
            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(progenyDefaultConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.IDP");
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            // Register the MediaDbContext database context with dependency injection.
            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(mediaDefaultConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.IDP");
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            // Register the ApplicationDbContext database context with dependency injection.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(authDefaultConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.IDP");
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
                options.UseOpenIddict(); // Add this line to enable OpenIddict support
            });

            return services;
        }
    }
}
