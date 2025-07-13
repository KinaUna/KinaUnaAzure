using IdentityModel;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using KinaUna.Data;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

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
            
            string storageConnectionString = Configuration["BlobStorageConnectionString"];
            new BlobContainerClient(storageConnectionString, "dataprotection").CreateIfNotExists();

            services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp")
                .PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");

            string authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            string authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            string authenticationServerClientSecret = Configuration.GetValue<string>("AuthenticationServerClientSecret");

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
            
            string progenyServerUrl = Configuration.GetValue<string>("ProgenyApiServer"); 
            string mediaServerUrl = Configuration.GetValue<string>("MediaApiServer");

            services.Configure<AuthConfigurations>(config => { config.StsServer = authorityServerUrl; config.ProtectedApiUrl = progenyServerUrl + " " + mediaServerUrl;});
            
            //services.AddCors(o =>
            //{
            //    if (_env.IsDevelopment())
            //    {
            //        o.AddDefaultPolicy(builder =>
            //        {
            //            builder.WithOrigins("https://*.kinauna.io", "https://nuuk2015.kinauna.io:44324",
            //                "https://nuuk2020.kinauna.io:44324", "https://nuuk2015.kinauna.io:44397",
            //                "https://nuuk2020.kinauna.io:44397", "https://nuuk2015.kinauna.io",
            //                "https://nuuk2020.kinauna.io").SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            //        });
            //    }

            //    o.AddPolicy("KinaUnaCors", builder =>
            //    {
            //        if (_env.IsDevelopment())
            //        {
            //            builder.WithOrigins("https://*.kinauna.io", "https://nuuk2015.kinauna.io:44324",
            //                    "https://nuuk2020.kinauna.io:44324", "https://nuuk2015.kinauna.io:44397",
            //                    "https://nuuk2020.kinauna.io:44397", "https://nuuk2015.kinauna.io",
            //                    "https://nuuk2020.kinauna.io").SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            //        }
            //        else
            //        {
            //            builder.WithOrigins("https://*." + Constants.AppRootDomain)
            //                .SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            //        }
            //    });
            //});
            
            services.AddControllersWithViews(options =>
            {
                AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson().AddViewLocalization()
            .AddRazorRuntimeCompilation();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = "KinaUnaCookie";
                    options.SlidingExpiration = true;
                    options.Events.OnSigningIn = (context) =>
                    {
                        context.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(30);
                        return Task.CompletedTask;
                    };

                    if (!_env.IsDevelopment())
                    {
                        options.Cookie.Domain = "web." + Constants.AppRootDomain;

                    }
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = authorityServerUrl;
                    options.ClientId = authenticationServerClientId;
                    options.ResponseType = OidcConstants.ResponseTypes.Code;
                    options.UsePkce = true;
                    options.RequireHttpsMetadata = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("roles");
                    options.Scope.Add("timezone");
                    options.Scope.Add("joindate");
                    options.Scope.Add("viewchild");
                    options.Scope.Add(Constants.ProgenyApiName);
                    options.Scope.Add(Constants.MediaApiName);
                    options.Scope.Add("offline_access");
                    options.SaveTokens = true;
                    options.ClientSecret = authenticationServerClientSecret;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.ClaimActions.Remove("amr");
                    options.ClaimActions.DeleteClaim("sid");
                    options.ClaimActions.DeleteClaim("idp");
                    options.ClaimActions.MapUniqueJsonKey("role", "role");
                    options.ClaimActions.MapUniqueJsonKey("timezone", "timezone");
                    options.ClaimActions.MapUniqueJsonKey("email", "email");
                    options.ClaimActions.MapUniqueJsonKey("email_verified", "email_verified");
                    options.ClaimActions.MapUniqueJsonKey("viewchild", "viewchild");
                    options.ClaimActions.MapUniqueJsonKey("joindate", "joindate");
                    options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
                    options.MapInboundClaims = false; // Prevents IdentityModel from mapping claims automatically, we want to use the original claim types from IdentityServer
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Email,
                        RoleClaimType = JwtClaimTypes.Role
                    };
                    
                });
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

            // app.UseCors("KinaUnaCors");
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
