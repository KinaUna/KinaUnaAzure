using KinaUna.Data.Contexts;
using KinaUnaWeb.HostingExtensions.Interfaces;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using Quartz;
using System;
using System.Security.Cryptography.X509Certificates;

namespace KinaUnaWeb.HostingExtensions
{
    public class OpenIddictConfiguration(
        string serverEncryptionCertificateThumbprint,
        string serverSigningCertificateThumbprint,
        string clientId,
        string clientSecret,
        string issuer,
        ICertificateProvider certificateProvider = null)
        : IOpenIddictConfigurator
    {
        private readonly ICertificateProvider _certificateProvider = certificateProvider ?? new DefaultCertificateProvider();

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureAuthentication(services);
            ConfigureQuartz(services);
            ConfigureOpenIddict(services);
        }

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(options =>
              {
                  options.LoginPath = "/login";
                  options.LogoutPath = "/logout";
                  options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
                  options.SlidingExpiration = false;
              });
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

                // Register the OpenIddict core components.
                .AddCore(options =>
                {
                    // Configure OpenIddict to use the Entity Framework Core stores and models.
                    // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                    options.UseEntityFrameworkCore()
                            .UseDbContext<ApplicationDbContext>();

                    // Developers who prefer using MongoDB can remove the previous lines
                    // and configure OpenIddict to use the specified MongoDB database:
                    // options.UseMongoDb()
                    //        .UseDatabase(new MongoClient().GetDatabase("openiddict"));

                    // Enable Quartz.NET integration.
                    options.UseQuartz();
                })

                // Register the OpenIddict client components.
                .AddClient(options =>
                {
                    // Note: this sample uses the code flow, but you can enable the other flows if necessary.
                    options.AllowAuthorizationCodeFlow();
                    
                    // Register the signing and encryption credentials used to protect
                    // sensitive data like the state tokens produced by OpenIddict.
                    ConfigureCertificates(options);

                    // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                    ConfigureAspNetCore(options);

                    // Register the System.Net.Http integration and use the identity of the current
                    // assembly as a more specific user agent, which can be useful when dealing with
                    // providers that use the user agent as a way to throttle requests (e.g Reddit).
                    options.UseSystemNetHttp()
                            .SetProductInformation(typeof(Startup).Assembly);

                    // Add a client registration matching the client application definition in the server project.
                    options.AddRegistration(new OpenIddictClientRegistration
                    {
                        Issuer = new Uri(issuer, UriKind.Absolute),

                        ClientId = clientId,
                        ClientSecret = clientSecret,
                        Scopes = { OpenIddictConstants.Permissions.Scopes.Email, OpenIddictConstants.Permissions.Scopes.Profile },

                        // Note: to mitigate mix-up attacks, it's recommended to use a unique redirection endpoint
                        // URI per provider, unless all the registered providers support returning a special "iss"
                        // parameter containing their URL as part of authorization responses. For more information,
                        // see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics#section-4.4.
                        RedirectUri = new Uri("callback/login/local", UriKind.Relative),
                        PostLogoutRedirectUri = new Uri("callback/logout/local", UriKind.Relative)
                    });
                }).AddValidation(options =>
                {
                    ConfigureValidationCertificates(options);
                    options.UseAspNetCore();
                });

            services.AddHostedService<OpenIddictWorkerService>();
        }
        
        private void ConfigureCertificates(OpenIddictClientBuilder options)
        {
            X509Certificate2 encryptionCertificate = _certificateProvider.GetCertificate(serverEncryptionCertificateThumbprint);
            X509Certificate2 signingCertificate = _certificateProvider.GetCertificate(serverSigningCertificateThumbprint);
            
            options.AddEncryptionCertificate(encryptionCertificate);
            options.AddSigningCertificate(signingCertificate);
        }

        private static void ConfigureAspNetCore(OpenIddictClientBuilder options)
        {
            options.UseAspNetCore()
                .EnableStatusCodePagesIntegration()
                .EnableRedirectionEndpointPassthrough()
                .EnablePostLogoutRedirectionEndpointPassthrough();
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
