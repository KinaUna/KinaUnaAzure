using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using KinaUnaWeb.HostingExtensions;
using KinaUnaWeb.HostingExtensions.Interfaces;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace KinaUnaWeb
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            TelemetryDebugWriter.IsTracingDisabled = true;

            _ = services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = delegate { return true; };
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.Secure = CookieSecurePolicy.Always;
            });

            string authOpenIddictClientConnection = Configuration["AuthOpenIddictClientConnection"] ?? throw new InvalidOperationException("AuthDefaultConnection was not found in the configuration data.");
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(authOpenIddictClientConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
                options.UseOpenIddict(); // Add this line to enable OpenIddict support
            });

            string storageConnectionString = Configuration["BlobStorageConnectionString"];
            new BlobContainerClient(storageConnectionString, "dataprotection").CreateIfNotExists();

            services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp")
                .PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<ILocaleManager, LocaleManager>();
            services.AddHttpClient();
            services.AddTransient<IProgenyManager, ProgenyManager>();
            services.AddHttpClient<IProgenyHttpClient, ProgenyHttpClient>();
            services.AddHttpClient<IMediaHttpClient, MediaHttpClient>();
            services.AddTransient<IIdentityParser<ApplicationUser>, IdentityParser>();
            services.AddSingleton<ImageStore>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<IPushMessageSender, PushMessageSender>();
            services.AddTransient<IWebNotificationsService, WebNotificationsService>();
            services.AddHttpClient<IWebNotificationsHttpClient, WebNotificationsHttpClient>();
            services.AddSingleton<ApiTokenInMemoryClient>();
            services.AddHttpClient<IUserInfosHttpClient, UserInfosHttpClient>();
            services.AddHttpClient<ITimelineHttpClient, TimelineHttpClient>();
            services.AddHttpClient<IWordsHttpClient, WordsHttpClient>();
            services.AddHttpClient<IVaccinationsHttpClient, VaccinationsHttpClient>();
            services.AddHttpClient<ISkillsHttpClient, SkillsHttpClient>();
            services.AddHttpClient<INotesHttpClient, NotesHttpClient>();
            services.AddHttpClient<IMeasurementsHttpClient, MeasurementsHttpClient>();
            services.AddHttpClient<ILocationsHttpClient, LocationsHttpClient>();
            services.AddHttpClient<IFriendsHttpClient, FriendsHttpClient>();
            services.AddHttpClient<IContactsHttpClient, ContactsHttpClient>();
            services.AddHttpClient<ICalendarsHttpClient, CalendarsHttpClient>();
            services.AddHttpClient<ISleepHttpClient, SleepHttpClient>();
            services.AddHttpClient<IUserAccessHttpClient, UserAccessHttpClient>();
            services.AddHttpClient<IAuthHttpClient, AuthHttpClient>();
            services.AddHttpClient<ILanguagesHttpClient, LanguagesHttpClient>();
            services.AddHttpClient<ITranslationsHttpClient, TranslationsHttpClient>();
            services.AddHttpClient<IPageTextsHttpClient, PageTextsHttpClient>();
            services.AddHttpClient<ITasksHttpClient, TasksHttpClient>();
            services.AddHttpClient<ICalendarRemindersHttpClient, CalendarRemindersHttpClient>();
            services.AddTransient<IViewModelSetupService, ViewModelSetupService>();
            services.AddTransient<ITimeLineItemsService, TimeLineItemsService>();
            services.AddHttpClient<IAutoSuggestsHttpClient, AutoSuggestsHttpClient>();
            services.AddDistributedMemoryCache();

            string authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            string authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            string authenticationServerClientSecret = Configuration.GetValue<string>("OpenIddictSecretString");
            string progenyServerUrl = Configuration.GetValue<string>("ProgenyApiServer"); 
            string mediaServerUrl = Configuration.GetValue<string>("MediaApiServer");

            // Register the OpenIddict services and configure them.
            string serverEncryptionCertificateThumbprint = Configuration["ServerEncryptionCertificateThumbprint"] ??
                                                           throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
            string serverSigningCertificateThumbprint = Configuration["ServerSigningCertificateThumbprint"] ??
                                                        throw new InvalidOperationException("ServerSigningCertificateThumbprint was not found in the configuration data.");
            
            services.AddSingleton<IOpenIddictConfigurator>(_ =>
            {
                OpenIddictConfiguration config = new(serverEncryptionCertificateThumbprint, serverSigningCertificateThumbprint, authenticationServerClientId, authenticationServerClientSecret, authorityServerUrl);
                config.ConfigureServices(services);
                return config;
            });

            services.Configure<AuthConfigurations>(config => { config.StsServer = authorityServerUrl; config.ProtectedApiUrl = progenyServerUrl + " " + mediaServerUrl;});
            
            services.AddControllersWithViews(options =>
            {
                AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson().AddViewLocalization();

            IMvcBuilder mvcBuilder = services.AddRazorPages();

            if (_env.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
            }
            
            services.AddAuthorization();
            services.AddSignalR().AddMessagePackProtocol().AddNewtonsoftJsonProtocol();
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            services.AddApplicationInsightsTelemetry();
        }

        public void Configure(IApplicationBuilder app)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Configuration["SyncfusionKey"]);
            
            app.UseCookiePolicy();
            CultureInfo[] supportedCultures =
            [
                new("en-US"),
                new("da-DK"),
                new("de-DE"),
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

            app.UseFileServer();

            app.UseRouting();

            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
                        
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WebNotificationHub>("/webnotificationhub");
                endpoints.MapDefaultControllerRoute();
            });            
        }
    }
}
