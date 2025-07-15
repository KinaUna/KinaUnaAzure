using System.Globalization;
using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.OpenIddict.HostingExtensions;
using KinaUna.OpenIddict.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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


builder.Services.AddDistributedMemoryCache();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ILocaleManager, LocaleManager>();
builder.Services.AddControllersWithViews();


// Register the OpenIddict services and configure them.
string serverEncryptionCertificateThumbprint = builder.Configuration["ServerEncryptionCertificateThumbprint"] ?? throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
string serverSigningCertificateThumbprint = builder.Configuration["ServerSigningCertificateThumbprint"] ?? throw new InvalidOperationException("ServerSigningCertificateThumbprint was not found in the configuration data.");

builder.Services.ConfigureOpenIddict(serverEncryptionCertificateThumbprint, serverSigningCertificateThumbprint);

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
