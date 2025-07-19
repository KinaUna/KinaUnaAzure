using KinaUna.Data.Contexts;
using KinaUna.OpenIddict.Services;
using OpenIddict.Abstractions;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Quartz;
using KinaUna.OpenIddict.HostingExtensions.Interfaces;

namespace KinaUna.OpenIddict.HostingExtensions
{
    public class OpenIddictConfiguration(
        string serverEncryptionCertificateThumbprint,
        string serverSigningCertificateThumbprint,
        ICertificateProvider? certificateProvider = null)
        : IOpenIddictConfigurator
    {
        private readonly ICertificateProvider _certificateProvider = certificateProvider ?? new DefaultCertificateProvider();

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureAuthentication(services);
            ConfigureIdentity(services);
            ConfigureQuartz(services);
            ConfigureOpenIddict(services);
        }

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
                    options.SlidingExpiration = true;
                });
        }

        private static void ConfigureIdentity(IServiceCollection services)
        {
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
        }

        private static void ConfigureQuartz(IServiceCollection services)
        {
            services.AddQuartz(options =>
            {
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        }

        private void ConfigureOpenIddict(IServiceCollection services)
        {
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
                    options.UseQuartz();
                })
                .AddServer(options =>
                {
                    ConfigureEndpoints(options);
                    ConfigureFlows(options);
                    ConfigureTokenLifetimes(options);
                    ConfigureScopes(options);
                    ConfigureCertificates(options);
                    ConfigureAspNetCore(options);
                })
                .AddValidation(options =>
                {
                    ConfigureValidationCertificates(options);
                    options.UseAspNetCore();
                });

            services.AddHostedService<OpenIddictSeeder>();
        }

        private static void ConfigureEndpoints(OpenIddictServerBuilder options)
        {
            options.SetAuthorizationEndpointUris("connect/authorize")
                .SetIntrospectionEndpointUris("connect/introspect")
                .SetEndSessionEndpointUris("connect/logout")
                .SetTokenEndpointUris("connect/token")
                .SetUserInfoEndpointUris("connect/userinfo")
                .SetEndUserVerificationEndpointUris("connect/verify");
        }

        private static void ConfigureFlows(OpenIddictServerBuilder options)
        {
            options.AllowAuthorizationCodeFlow()
                .AllowClientCredentialsFlow()
                .AllowRefreshTokenFlow();
        }

        private static void ConfigureTokenLifetimes(OpenIddictServerBuilder options)
        {
            options.SetAccessTokenLifetime(TimeSpan.FromSeconds(300))
                .SetRefreshTokenLifetime(TimeSpan.FromDays(30));
        }

        private static void ConfigureScopes(OpenIddictServerBuilder options)
        {
            options.RegisterScopes(OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.OfflineAccess, 
                Constants.ProgenyApiName, 
                Constants.MediaApiName);
        }

        private void ConfigureCertificates(OpenIddictServerBuilder options)
        {
            X509Certificate2 encryptionCertificate = _certificateProvider.GetCertificate(serverEncryptionCertificateThumbprint);
            X509Certificate2 signingCertificate = _certificateProvider.GetCertificate(serverSigningCertificateThumbprint);
            
            options.AddEncryptionCertificate(encryptionCertificate);
            options.AddSigningCertificate(signingCertificate);
        }

        private static void ConfigureAspNetCore(OpenIddictServerBuilder options)
        {
            options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .EnableStatusCodePagesIntegration();
        }

        private void ConfigureValidationCertificates(OpenIddictValidationBuilder options)
        {
            X509Certificate2 encryptionCertificate = _certificateProvider.GetCertificate(serverEncryptionCertificateThumbprint);
            X509Certificate2 signingCertificate = _certificateProvider.GetCertificate(serverSigningCertificateThumbprint);
            
            options.AddEncryptionCertificate(encryptionCertificate);
            options.AddSigningCertificate(signingCertificate);
        }
    }
}
