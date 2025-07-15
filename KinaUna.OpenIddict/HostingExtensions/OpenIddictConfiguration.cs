using KinaUna.Data.Contexts;
using KinaUna.OpenIddict.Services;
using OpenIddict.Abstractions;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data;

namespace KinaUna.OpenIddict.HostingExtensions
{
    internal static class OpenIddictConfiguration
    {
        public static IServiceCollection ConfigureOpenIddict(this IServiceCollection services, string serverEncryptionCertificateThumbprint, string serverSigningCertificateThumbprint)
        {
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
                    options.UseQuartz(); // For token cleanup
                })
                .AddServer(options =>
                {
                    // Todo: Find out if more configuration is needed here.
                    options.SetIntrospectionEndpointUris("connect/introspect")
                        .SetEndSessionEndpointUris("connect/logout")
                        .SetTokenEndpointUris("connect/token")
                        .SetUserInfoEndpointUris("connect/userinfo")
                        .SetEndUserVerificationEndpointUris("connect/verify");

                    options.AllowAuthorizationCodeFlow()
                        .AllowClientCredentialsFlow()
                        .AllowRefreshTokenFlow();

                    options.SetAccessTokenLifetime(TimeSpan.FromSeconds(300))
                        .SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                    options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles,
                        OpenIddictConstants.Scopes.OfflineAccess, Constants.ProgenyApiName, Constants.MediaApiName);


                    // Todo: Find out if more configuration is needed here.
                    options.UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableEndSessionEndpointPassthrough()
                        .EnableTokenEndpointPassthrough()
                        .EnableUserInfoEndpointPassthrough()
                        .EnableStatusCodePagesIntegration();

                })
                .AddValidation(options =>
                {
                    // The certificates used need to be added to the certificate store.
                    // For Azure App Service the certificates must be uploaded to the App Service.
                    // For Azure Windows App Services the certificates must be made accessible: https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code?tabs=windows#make-the-certificate-accessible
                    // For local development, the certificates can be added to the CurrentUser store.
                    options.AddEncryptionCertificate(serverEncryptionCertificateThumbprint, StoreName.My, StoreLocation.CurrentUser);
                    options.AddSigningCertificate(serverSigningCertificateThumbprint, StoreName.My, StoreLocation.CurrentUser);
                    options.UseAspNetCore();
                });


            // Seed the database with initial data.
            services.AddHostedService<OpenIddictSeeder>();

            return services;
        }
    }
}
