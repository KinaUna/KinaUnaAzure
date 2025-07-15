using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.OpenIddict.HostingExtensions;
using KinaUna.OpenIddict.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace KinaUna.OpenIddict.Tests.HostingExtensions
{
    public class OpenIddictConfigurationTests
    {
        [Fact]
        public void ConfigureOpenIddict_RegistersAllRequiredServices()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Verify authentication is configured
            IAuthenticationService authService = serviceProvider.GetRequiredService<IAuthenticationService>();
            Assert.NotNull(authService);

            // Verify cookie authentication is registered
            IAuthenticationSchemeProvider authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
            IEnumerable<AuthenticationScheme> schemes = authSchemeProvider.GetAllSchemesAsync().GetAwaiter().GetResult();
            Assert.Contains(schemes, s => s.Name == CookieAuthenticationDefaults.AuthenticationScheme);

            // Verify Identity is configured
            UserManager<ApplicationUser>? userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            Assert.NotNull(userManager);

            // Verify Quartz is configured
            IHostedService? quartzHostedService = serviceProvider.GetServices<IHostedService>()
                .FirstOrDefault(s => s.GetType().Name.Contains("Quartz"));
            Assert.NotNull(quartzHostedService);

            // Verify OpenIddict is configured
            IOpenIddictApplicationManager? openIddictApplicationManager = serviceProvider.GetService<IOpenIddictApplicationManager>();
            Assert.NotNull(openIddictApplicationManager);

            // Verify OpenIddictSeeder is registered as a hosted service
            IHostedService? seederService = serviceProvider.GetServices<IHostedService>()
                .FirstOrDefault(s => s is OpenIddictSeeder);
            Assert.NotNull(seederService);
        }

        [Fact]
        public void ConfigureOpenIddict_ConfiguresIdentityWithCorrectOptions()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Add required services for Identity
            services.AddLogging();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddOptions();
            services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IdentityOptions identityOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Value;

            // Verify identity options are set correctly
            Assert.False(identityOptions.Password.RequireNonAlphanumeric);
            Assert.False(identityOptions.Password.RequireUppercase);
            Assert.False(identityOptions.Password.RequireLowercase);
            Assert.False(identityOptions.Password.RequireDigit);
            Assert.True(identityOptions.SignIn.RequireConfirmedEmail);
            Assert.True(identityOptions.User.RequireUniqueEmail);
        }

        [Fact]
        public void ConfigureOpenIddict_ConfiguresOpenIddictWithCorrectScopes()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Mock minimal EF Core setup for ApplicationDbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OpenIddictTest"));

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert - Check OpenIddict Server options
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            // OpenIddictCoreBuilder openIddictBuilder = serviceProvider.GetRequiredService<OpenIddictCoreBuilder>();

            // Verify server options by checking registered services
            ServiceDescriptor? serverOptions = services.FirstOrDefault(d => d.ServiceType == typeof(OpenIddictServerOptions));
            Assert.NotNull(serverOptions);

            // Verify scopes were registered properly
            OpenIddictServerOptions options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenIddictServerOptions>>().Value;

            Assert.Contains(options.Scopes, s => s == OpenIddictConstants.Scopes.Email);
            Assert.Contains(options.Scopes, s => s == OpenIddictConstants.Scopes.Profile);
            Assert.Contains(options.Scopes, s => s == OpenIddictConstants.Scopes.Roles);
            Assert.Contains(options.Scopes, s => s == OpenIddictConstants.Scopes.OfflineAccess);
            Assert.Contains(options.Scopes, s => s == Constants.ProgenyApiName);
            Assert.Contains(options.Scopes, s => s == Constants.MediaApiName);
        }

        [Fact]
        public void ConfigureOpenIddict_ConfiguresTokenLifetimes()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Mock minimal EF Core setup for ApplicationDbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OpenIddictTestTokens"));

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            OpenIddictServerOptions options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenIddictServerOptions>>().Value;

            // Verify token lifetimes
            Assert.Equal(TimeSpan.FromSeconds(300), options.AccessTokenLifetime);
            Assert.Equal(TimeSpan.FromDays(30), options.RefreshTokenLifetime);
        }

        [Fact]
        public void ConfigureOpenIddict_AddsCertificatesForTokenSecurity()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert - Verify validation options
            ServiceDescriptor? validationOptions = services.FirstOrDefault(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<OpenIddictValidationOptions>));
            Assert.NotNull(validationOptions);

            // Can't directly test certificate loading since it requires access to certificate store
            // but we can verify the registration of the options
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            OpenIddictValidationOptions options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenIddictValidationOptions>>().Value;
            Assert.NotNull(options);
        }

        [Fact]
        public void ConfigureOpenIddict_ConfiguresAuthenticationCookieOptions()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            CookieAuthenticationOptions cookieOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CookieAuthenticationOptions>>().Value;

            Assert.Equal("/login", cookieOptions.LoginPath);
            Assert.Equal("/logout", cookieOptions.LogoutPath);
            Assert.Equal(TimeSpan.FromDays(60), cookieOptions.ExpireTimeSpan);
            Assert.True(cookieOptions.SlidingExpiration);
        }

        [Fact]
        public void ConfigureOpenIddict_ConfiguresAllRequiredEndpoints()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Mock minimal EF Core setup for ApplicationDbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OpenIddictTestEndpoints"));

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            OpenIddictServerOptions options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenIddictServerOptions>>().Value;

            // Verify endpoints
            Assert.Contains(options.IntrospectionEndpointUris, uri => uri.ToString() == "connect/introspect");
            Assert.Contains(options.EndSessionEndpointUris, uri => uri.ToString() == "connect/logout");
            Assert.Contains(options.TokenEndpointUris, uri => uri.ToString() == "connect/token");
            Assert.Contains(options.UserInfoEndpointUris, uri => uri.ToString() == "connect/userinfo");
            Assert.Contains(options.EndUserVerificationEndpointUris, uri => uri.ToString() == "connect/verify");
        }

        [Fact]
        public void ConfigureOpenIddict_ConfiguresAuthorizedFlows()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            string encryptionThumbprint = "1234567890ABCDEF1234567890ABCDEF12345678";
            string signingThumbprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF12";

            // Mock minimal EF Core setup for ApplicationDbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OpenIddictTestFlows"));

            // Act
            services.ConfigureOpenIddict(encryptionThumbprint, signingThumbprint);

            // Assert
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            OpenIddictServerOptions options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenIddictServerOptions>>().Value;

            // Verify authorized flows
            Assert.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode, options.GrantTypes);
            Assert.Contains(OpenIddictConstants.GrantTypes.ClientCredentials, options.GrantTypes);
            Assert.Contains(OpenIddictConstants.GrantTypes.RefreshToken, options.GrantTypes);
        }
    }
}