using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.OpenIddict.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Quartz;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add database context and other services.

// Register the ProgenyDbContext database context with dependency injection.
builder.Services.AddDbContext<ProgenyDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ProgenyDefaultConnection"],
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("KinaUna.IDP");
            //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }));

// Register the MediaDbContext database context with dependency injection.
builder.Services.AddDbContext<MediaDbContext>(options =>
    options.UseSqlServer(builder.Configuration["MediaDefaultConnection"],
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("KinaUna.IDP");
            //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }));

// Register the ApplicationDbContext database context with dependency injection.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration["AuthDefaultConnection"],
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("KinaUna.IDP");
            //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        });
    options.UseOpenIddict(); // Add this line to enable OpenIddict support
});


string storageConnectionString = builder.Configuration["BlobStorageConnectionString"] ?? throw new InvalidOperationException("BlobStorageConnectionString was not found in the configuration data.");
new BlobContainerClient(storageConnectionString, "dataprotection").CreateIfNotExists();

builder.Services.AddDataProtection()
    .SetApplicationName("KinaUnaWebApp")
    .PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");

builder.Services.AddDistributedMemoryCache();

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ILocaleManager, LocaleManager>();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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

builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddOpenIddict()
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
        options.AddEncryptionCertificate(builder.Configuration["ServerEncryptionCertificateThumbprint"] 
                                         ?? throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data."), StoreName.My, StoreLocation.CurrentUser);
        options.AddSigningCertificate(builder.Configuration["ServerSigningCertificateThumbprint"]
                                      ?? throw new InvalidOperationException("ServerSigningCertificateThumbprint was not found in the configuration data."), StoreName.My, StoreLocation.CurrentUser);
        options.UseAspNetCore();
    });

// Seed the database with initial data.
builder.Services.AddHostedService<OpenIddictSeeder>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

CultureInfo[] supportedCultures =
[
    new CultureInfo("en-US"),
    new CultureInfo("da-DK"),
    new CultureInfo("de-DE"),
];

RequestLocalizationOptions localizationOptions = new()
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
CookieRequestCultureProvider provider = new()
{
    CookieName = Constants.LanguageCookieName
};
localizationOptions.RequestCultureProviders.Insert(0, provider);
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.UseEndpoints(options =>
{
    _ = options.MapControllers();
    _ = options.MapDefaultControllerRoute();
});

app.Run();
