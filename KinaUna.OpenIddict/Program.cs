using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Utilities;
using KinaUna.OpenIddict.AuthorizationHandlers;
using KinaUna.OpenIddict.HostingExtensions;
using KinaUna.OpenIddict.Services;
using KinaUna.OpenIddict.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Quartz;
using Serilog;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = delegate { return false; };
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "KinaUna.OpenIddict")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["SeqUrl"] ?? "http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();

AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();
// Add database context and other services.

string progenyDefaultConnection = builder.Configuration["ProgenyDefaultConnection"] 
                                  ?? throw new InvalidOperationException("ProgenyDefaultConnection was not found in the configuration data.");
string mediaDefaultConnection = builder.Configuration["MediaDefaultConnection"] 
                                ?? throw new InvalidOperationException("MediaDefaultConnection was not found in the configuration data.");
string authDefaultConnection = builder.Configuration["AuthDefaultConnection"] 
                               ?? throw new InvalidOperationException("AuthDefaultConnection was not found in the configuration data.");

builder.Services.ConfigureDatabases(progenyDefaultConnection, mediaDefaultConnection, authDefaultConnection);


string keyPath = builder.Configuration.GetValue<string>("DataProtectionKeyPath") ?? "/app/storage/dataprotection";
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection()
    .SetApplicationName("KinaUnaWebApp")
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ILocaleManager, LocaleManager>();
builder.Services.AddTransient<IProgenyApiHttpClient, ProgenyApiHttpClient>();
builder.Services.AddSingleton<ITokenService, TokenService>();

string authenticationServerClientId = builder.Configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey) 
                                      ?? throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + " was not found in the configuration data.");
string authenticationServerClientSecret = builder.Configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey) 
                                          ?? throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + " was not found in the configuration data.");
if (builder.Environment.IsDevelopment())
{
    // In development, use the local URLs for the Progeny API and Web Server.
    authenticationServerClientId = builder.Configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Local") 
                                      ?? throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Local was not found in the configuration data.");
    authenticationServerClientSecret = builder.Configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Local") 
                                       ?? throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Local was not found in the configuration data.");
}

if (builder.Environment.IsStaging())
{
    authenticationServerClientId = builder.Configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Azure")
                                   ?? throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Azure was not found in the configuration data.");
    authenticationServerClientSecret = builder.Configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Azure")
                                       ?? throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Azure was not found in the configuration data.");
}
//Register the OpenIddict services and configure them.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
        options.ClaimsIdentity.EmailClaimType = Claims.Email;
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Load certificate thumbprints from configuration data. These thumbprints are used to load the encryption and signing certificates from the certificate store.
// In production, these thumbprints should be set to the thumbprints of the certificates that are installed in the certificate store on the server.
// In development, these thumbprints can be set to the thumbprints of the certificates that are installed in the certificate store on the local machine.
// If the thumbprints are not provided, the application will throw an exception at startup, as the certificates are required for OpenIddict to function properly.
string serverEncryptionCertificateThumbprint = builder.Configuration["ServerEncryptionCertificateThumbprint"] 
                                               ?? throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
string serverSigningCertificateThumbprint = builder.Configuration["ServerSigningCertificateThumbprint"] 
                                            ?? throw new InvalidOperationException("ServerSigningCertificateThumbprint was not found in the configuration data.");

// In Docker/Linux environments, load certificates from PFX files instead of the certificate store.
// The PFX file paths and passwords are specified in the configuration data (e.g., appsettings.json or environment variables).
// If the PFX paths are not provided, the application will fall back to loading certificates from the certificate store using the specified thumbprints.
string? encryptionPfxPath = builder.Configuration["EncryptionCertificatePfxPath"];
string? encryptionPfxPassword = builder.Configuration["EncryptionCertificatePfxPassword"];
string? signingPfxPath = builder.Configuration["SigningCertificatePfxPath"];
string? signingPfxPassword = builder.Configuration["SigningCertificatePfxPassword"];

X509Certificate2 encryptionCertificate = !string.IsNullOrEmpty(encryptionPfxPath)
    ? CertificateTools.GetCertificateFromPfxFile(encryptionPfxPath, encryptionPfxPassword)
    : CertificateTools.GetCertificate(serverEncryptionCertificateThumbprint);
X509Certificate2 signingCertificate = !string.IsNullOrEmpty(signingPfxPath)
    ? CertificateTools.GetCertificateFromPfxFile(signingPfxPath, signingPfxPassword)
    : CertificateTools.GetCertificate(serverSigningCertificateThumbprint);

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
            .SetIntrospectionEndpointUris("connect/introspect")
            .SetEndSessionEndpointUris("connect/logout")
            .SetTokenEndpointUris("connect/token")
            .SetUserInfoEndpointUris("connect/userinfo")
            .SetEndUserVerificationEndpointUris("connect/verify");

        // Flows
        options.AllowClientCredentialsFlow()
            .AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .AllowTokenExchangeFlow()
            .RequireProofKeyForCodeExchange();

        options.RegisterScopes(
            Scopes.Email,
            Scopes.Profile,
            Scopes.OpenId,
            Scopes.Roles,
            Scopes.OfflineAccess,
            AuthConstants.ProgenyApiName,
            AuthConstants.ProgenyApiName + "azure",
            AuthConstants.ProgenyApiName + "local",
            AuthConstants.AuthApiName,
            AuthConstants.AuthApiName + "azure",
            AuthConstants.AuthApiName + "local");
        
        options.AddEncryptionCertificate(encryptionCertificate);
        options.AddSigningCertificate(signingCertificate);
        options.DisableAccessTokenEncryption();
        
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(30));
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.SetClientId(authenticationServerClientId);
        options.SetClientSecret(authenticationServerClientSecret);
        options.UseAspNetCore();
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Client", policy => { policy.Requirements.Add(new ClientRequirement()); });
builder.Services.AddSingleton<IAuthorizationHandler, ClientHandler>();
builder.Services.AddAuthorization();

// Register the OpenIddict seeder service to initialize the OpenIddict database with necessary data.
builder.Services.AddScoped<IClientConfigProvider, ConfigurationClientConfigProvider>();
builder.Services.AddHostedService<OpenIddictSeeder>();

// Configure CORS to allow requests from the specified origin.
// Additional origins can be provided via the CorsOrigins configuration key (semicolon-separated).
string[] rawConfiguredOrigins = builder.Configuration.GetValue<string>("CorsOrigins")
    ?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];

string[] configuredOrigins = rawConfiguredOrigins
    .Select(origin => origin.Trim())
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

// If development, allow any origin.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins([.. Constants.DevelopmentCorsList, .. configuredOrigins])));
}
else
{
    if (builder.Environment.IsStaging())
    {
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins([.. Constants.StagingCorsList, .. configuredOrigins])));
    }
    else
    {
        // In production, only allow requests from the specified origin.
        // This is important for security reasons.
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins([.. Constants.ProductionCorsList, .. configuredOrigins])));
    }
}

builder.Services.AddControllersWithViews().AddViewLocalization();

builder.Services.AddRazorPages();

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/error");

app.UseCors();
app.UseHttpsRedirection();
app.UseRouting();

CultureInfo[] supportedCultures =
[
    new("en-US"),
    new("da-DK"),
    new("de-DE"),
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

app.UseFileServer();
app.UseEndpoints(options =>
{
    _ = options.MapControllers();
    _ = options.MapDefaultControllerRoute();
    _ = options.MapHealthChecks("/health").AllowAnonymous();
});

app.Run();
