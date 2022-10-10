using System.Globalization;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using IdentityModel;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaWebBlazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Tokens;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = delegate { return true; };
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});

// Add services to the container.
builder.Services.AddDataProtection()
    .SetApplicationName("KinaUnaWebApp")
    .PersistKeysToAzureBlobStorage(builder.Configuration.GetValue<string>("kinaunastorageconnectionstring"), "dataprotection", "kukeys.xml");

builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetValue<string>("WebDefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("KinaUna.IDP");
            //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }));

string authorityServerUrl = builder.Configuration.GetValue<string>("AuthenticationServer");
if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
{
    authorityServerUrl = builder.Configuration.GetValue<string>("AuthenticationServer" + Constants.DebugKinaUnaServer);
}

string authenticationServerClientId = builder.Configuration.GetValue<string>("AuthenticationServerClientId");
if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
{
    authenticationServerClientId = builder.Configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer);
}

string authenticationServerClientSecret = builder.Configuration.GetValue<string>("AuthenticationServerClientSecret");

string progenyServerUrl = builder.Configuration.GetValue<string>("ProgenyApiServer");
if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
{
    progenyServerUrl = builder.Configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer);
}

string mediaServerUrl = builder.Configuration.GetValue<string>("MediaApiServer");
if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
{
    mediaServerUrl = builder.Configuration.GetValue<string>("MediaApiServer" + Constants.DebugKinaUnaServer);
}

builder.Services.Configure<AuthConfigurations>(config =>
{
    config.StsServer = authorityServerUrl;
    config.ProtectedApiUrl = progenyServerUrl + " " + mediaServerUrl;
});

builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<ILocaleManager, LocaleManager>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IProgenyManager, ProgenyManager>();
builder.Services.AddHttpClient<IProgenyHttpClient, ProgenyHttpClient>();
builder.Services.AddHttpClient<IMediaHttpClient, MediaHttpClient>();
builder.Services.AddTransient<IIdentityParser<ApplicationUser>, IdentityParser>();
builder.Services.AddSingleton<ImageStore>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<IPushMessageSender, PushMessageSender>();
builder.Services.AddSingleton<ApiTokenInMemoryClient>();
builder.Services.AddHttpClient<IUserInfosHttpClient, UserInfosHttpClient>();
builder.Services.AddHttpClient<ITimelineHttpClient, TimelineHttpClient>();
builder.Services.AddHttpClient<IWordsHttpClient, WordsHttpClient>();
builder.Services.AddHttpClient<IVaccinationsHttpClient, VaccinationsHttpClient>();
builder.Services.AddHttpClient<ISkillsHttpClient, SkillsHttpClient>();
builder.Services.AddHttpClient<INotesHttpClient, NotesHttpClient>();
builder.Services.AddHttpClient<IMeasurementsHttpClient, MeasurementsHttpClient>();
builder.Services.AddHttpClient<ILocationsHttpClient, LocationsHttpClient>();
builder.Services.AddHttpClient<IFriendsHttpClient, FriendsHttpClient>();
builder.Services.AddHttpClient<IContactsHttpClient, ContactsHttpClient>();
builder.Services.AddHttpClient<ICalendarsHttpClient, CalendarsHttpClient>();
builder.Services.AddHttpClient<ISleepHttpClient, SleepHttpClient>();
builder.Services.AddHttpClient<IUserAccessHttpClient, UserAccessHttpClient>();
builder.Services.AddHttpClient<IAuthHttpClient, AuthHttpClient>();
builder.Services.AddHttpClient<ILanguagesHttpClient, LanguagesHttpClient>();
builder.Services.AddHttpClient<ITranslationsHttpClient, TranslationsHttpClient>();
builder.Services.AddHttpClient<IPageTextsHttpClient, PageTextsHttpClient>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddScoped<UserStateService>();
builder.Services.AddDistributedMemoryCache();

builder.Services
    .AddBlazorise(options => { options.Immediate = true; })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthorization();
//builder.Services.AddAuthorizationCore();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthentication(sharedOptions =>
    {
        sharedOptions.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
        sharedOptions.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
        sharedOptions.DefaultChallengeScheme =
            OpenIdConnectDefaults.AuthenticationScheme;
    }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "KinaUnaCookie";
    options.SlidingExpiration = true;
    options.Events.OnSigningIn = (context) =>
    {
        context.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(30);
        context.CookieOptions.IsEssential = true;
        return Task.CompletedTask;
    };

    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.Domain = "web." + Constants.AppRootDomain;

    }
    else
    {
        if (!string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
        {
            options.Cookie.Domain = ".kinauna.io";
        }
    }
})
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = authorityServerUrl;
                    options.ClientId = authenticationServerClientId;
                    options.ResponseType = "code id_token";
                    options.UsePkce = false;
                    options.RequireHttpsMetadata = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add(Constants.ProgenyApiName);
                    options.Scope.Add(Constants.MediaApiName);
                    options.Scope.Add("offline_access");
                    options.SaveTokens = true;
                    options.ClientSecret = authenticationServerClientSecret;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.ClaimActions.Remove("amr");
                    options.ClaimActions.DeleteClaim("sid");
                    options.ClaimActions.DeleteClaim("idp");
                    options.ClaimActions.MapUniqueJsonKey("email", "email");
                    options.ClaimActions.MapUniqueJsonKey("email_verified", "email_verified");
                    options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Email
                    };
                    options.Events = new OpenIdConnectEvents
                    {
                        OnAccessDenied = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/");
                            return Task.CompletedTask;
                        }
                    };
                });

WebApplication app = builder.Build();

app.UseCookiePolicy();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

CultureInfo[] supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("da-DK"),
    new CultureInfo("de-DE"),
};

RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

CookieRequestCultureProvider provider = new CookieRequestCultureProvider()
{
    CookieName = Constants.LanguageCookieName
};
localizationOptions.RequestCultureProviders.Insert(0, provider);

app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub(option =>
    {
        option.CloseOnAuthenticationExpiration = true; //This option is used to enable authentication expiration tracking which will close connections when a token expires
    });
    endpoints.MapFallbackToPage("/_Host");
});

app.Run();
