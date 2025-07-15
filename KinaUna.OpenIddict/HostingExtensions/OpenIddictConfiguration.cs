using KinaUna.Data.Contexts;
using KinaUna.OpenIddict.Services;
using OpenIddict.Abstractions;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Quartz;

namespace KinaUna.OpenIddict.HostingExtensions
{
    internal static class OpenIddictConfiguration
    {
        /// <summary>
        /// Configures OpenIddict services for authentication and authorization in the application.
        /// </summary>
        /// <remarks>This method sets up authentication using cookies, configures ASP.NET Identity, and
        /// integrates Quartz.NET for job scheduling. It also configures OpenIddict to support various OAuth 2.0 flows
        /// and endpoints, and sets token lifetimes. Certificates for encryption and signing must be properly configured
        /// in the environment where the application is running.</remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the OpenIddict services are added.</param>
        /// <param name="serverEncryptionCertificateThumbprint">The thumbprint of the certificate used for encrypting tokens. This certificate must be accessible in the
        /// certificate store.</param>
        /// <param name="serverSigningCertificateThumbprint">The thumbprint of the certificate used for signing tokens. This certificate must be accessible in the
        /// certificate store.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> with OpenIddict services configured.</returns>
        public static IServiceCollection ConfigureOpenIddict(this IServiceCollection services, string serverEncryptionCertificateThumbprint, string serverSigningCertificateThumbprint)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(60);
                    options.SlidingExpiration = true;
                });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireDigit = false;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddQuartz(options =>
            {
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);


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
