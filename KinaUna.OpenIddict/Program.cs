using System.Globalization;
using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

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
    options.UseSqlServer(builder.Configuration["DefaultConnection"],
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("KinaUna.IDP");
            //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }));

string storageConnectionString = builder.Configuration["BlobStorageConnectionString"] ?? throw new InvalidOperationException("BlobStorageConnectionString was not found in the configuration data.");
new BlobContainerClient(storageConnectionString, "dataprotection").CreateIfNotExists();

builder.Services.AddDataProtection()
    .SetApplicationName("KinaUnaWebApp")
    .PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");

builder.Services.AddDistributedMemoryCache();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => { options.LoginPath = "/account/login"; });

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
        options.UseQuartz(); // For token cleanup
    })
    .AddServer(options =>
    {
        // Todo: Find out if more configuration is needed here.
        options.SetTokenEndpointUris("connect/token");

        options.AllowAuthorizationCodeFlow()
            .AllowClientCredentialsFlow()
            .AllowRefreshTokenFlow();

        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(2592000))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        options.RegisterScopes("openid", "profile", "email", "offline_access",
            "roles", "timezone", "viewchild", "joindate",
            Constants.ProgenyApiName, Constants.MediaApiName);

        // Todo: Find out if more configuration is needed here.
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
    });

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
