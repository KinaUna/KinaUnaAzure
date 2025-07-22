using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Utilities;
using KinaUna.OpenIddict.HostingExtensions;
using KinaUna.OpenIddict.Services;
using KinaUna.OpenIddict.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Quartz;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = delegate { return true; };
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});

// Add database context and other services.

string progenyDefaultConnection = builder.Configuration["ProgenyDefaultConnection"] ?? throw new InvalidOperationException("ProgenyDefaultConnection was not found in the configuration data.");
string mediaDefaultConnection = builder.Configuration["MediaDefaultConnection"] ?? throw new InvalidOperationException("MediaDefaultConnection was not found in the configuration data.");
string authDefaultConnection = builder.Configuration["AuthDefaultConnection"] ?? throw new InvalidOperationException("AuthDefaultConnection was not found in the configuration data.");

builder.Services.ConfigureDatabases(progenyDefaultConnection, mediaDefaultConnection, authDefaultConnection);


string storageConnectionString = builder.Configuration["BlobStorageConnectionString"] ?? throw new InvalidOperationException("BlobStorageConnectionString was not found in the configuration data.");
new BlobContainerClient(storageConnectionString, "dataprotection").CreateIfNotExists();

builder.Services.AddDataProtection()
    .SetApplicationName("KinaUnaWebApp")
    .PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");

builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ILocaleManager, LocaleManager>();

builder.Services.AddControllersWithViews().AddViewLocalization();

IMvcBuilder mvcBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

// Register the OpenIddict services and configure them.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        options.ClaimsIdentity.UserNameClaimType = Claims.Name;
        options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
        options.ClaimsIdentity.RoleClaimType = Claims.Role;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

string serverEncryptionCertificateThumbprint =
    builder.Configuration["ServerEncryptionCertificateThumbprint"] ?? throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
string serverSigningCertificateThumbprint =
    builder.Configuration["ServerSigningCertificateThumbprint"] ?? throw new InvalidOperationException("ServerSigningCertificateThumbprint was not found in the configuration data.");
X509Certificate2 encryptionCertificate = CertificateTools.GetCertificate(serverEncryptionCertificateThumbprint);
X509Certificate2 signingCertificate = CertificateTools.GetCertificate(serverSigningCertificateThumbprint);

// string progenyServerUrl = builder.Configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer was not found in the configuration data.");
// string webServerUrl = builder.Configuration.GetValue<string>("WebServer") ?? throw new InvalidOperationException("WebServer was not found in the configuration data.");
// string progenyServerLocalUrl = builder.Configuration.GetValue<string>("ProgenyApiServerLocal") ?? throw new InvalidOperationException("ProgenyApiServerLocal was not found in the configuration data.");
// string webServerLocalUrl = builder.Configuration.GetValue<string>("WebServerLocal") ?? throw new InvalidOperationException("WebServerLocal was not found in the configuration data.");

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
        options.UseQuartz();
    })
    .AddServer(options =>
    {
        // Token endpoint
        options.SetAuthorizationEndpointUris("connect/authorize")
            //.SetDeviceEndpointUris("connect/device")
            .SetIntrospectionEndpointUris("connect/introspect")
            .SetEndSessionEndpointUris("connect/logout")
            .SetTokenEndpointUris("connect/token")
            .SetUserInfoEndpointUris("connect/userinfo")
            .SetEndUserVerificationEndpointUris("connect/verify");

        // Flows
        options.AllowClientCredentialsFlow()
            .AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .RequireProofKeyForCodeExchange(); // PKCE
        
        options.RegisterScopes(
            Scopes.Email,
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess,
            Constants.ProgenyApiName,
            Constants.ProgenyApiName + "azure",
            Constants.ProgenyApiName + "local");

        //options.UseReferenceAccessTokens();
        //options.UseReferenceRefreshTokens();

        options.AddEncryptionCertificate(encryptionCertificate);
        options.AddSigningCertificate(signingCertificate);
        options.DisableAccessTokenEncryption();
        
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(30));
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        //options.AddEncryptionCertificate(encryptionCertificate);
        //options.AddSigningCertificate(signingCertificate);
        options.UseAspNetCore();
    });

// Register the OpenIddict seeder service to initialize the OpenIddict database with necessary data.
builder.Services.AddScoped<IClientConfigProvider, ConfigurationClientConfigProvider>();
builder.Services.AddHostedService<OpenIddictSeeder>();

// Configure CORS to allow requests from the specified origin.
// If development, allow any origin.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(Constants.DevelopmentCorsList)));
}
// If production, restrict to the specified origin.
else
{
    // In production, only allow requests from the specified origin.
    // This is important for security reasons.
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(Constants.ProductionCorsList)));
}

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseRouting();

CultureInfo[] supportedCultures =
[
    new CultureInfo("en-US"),
    new CultureInfo("da-DK"),
    new CultureInfo("de-DE"),
];

app.UseCookiePolicy();

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
app.UseStaticFiles();
app.UseEndpoints(options =>
{
    _ = options.MapControllers();
    _ = options.MapDefaultControllerRoute();
});

app.Run();
