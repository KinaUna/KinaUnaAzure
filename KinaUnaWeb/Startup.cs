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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using KinaUna.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;

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

            _ = services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = delegate { return true; };
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.Secure = CookieSecurePolicy.Always;
            });

            services.AddDbContext<WebDbContext>(options =>
                options.UseSqlServer(Configuration["WebDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.IDP");
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));


            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration["DataProtectionConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            StorageCredentials credentials = new StorageCredentials(Constants.CloudBlobUsername, Configuration["BlobStorageKey"]);
            CloudBlobClient blobClient = new CloudBlobClient(new Uri(Constants.CloudBlobBase), credentials);
            CloudBlobContainer container = blobClient.GetContainerReference("dataprotection");

            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp")
                .PersistKeysToAzureBlobStorage(container, "kukeys.xml");

            string authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer" + Constants.DebugKinaUnaServer);
            }
            string authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer);
            }
            string authenticationServerClientSecret = Configuration.GetValue<string>("AuthenticationServerClientSecret");

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<ILocaleManager, LocaleManager>();
            services.AddHttpClient();
            services.AddHttpClient<IProgenyManager, ProgenyManager>();
            services.AddHttpClient<IProgenyHttpClient, ProgenyHttpClient>();
            services.AddHttpClient<IMediaHttpClient, MediaHttpClient>();
            services.AddTransient<IIdentityParser<ApplicationUser>, IdentityParser>();
            services.AddSingleton<ImageStore>();
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<IPushMessageSender, PushMessageSender>();
            services.AddTransient<IWebNotificationsService, WebNotificationsService>();
            services.AddHttpClient<INotificationsHttpClient, NotificationsHttpClient>();
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
            services.AddDistributedMemoryCache();

            string progenyServerUrl = Configuration.GetValue<string>("ProgenyApiServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                progenyServerUrl = Configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer);
            }

            string mediaServerUrl = Configuration.GetValue<string>("MediaApiServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                mediaServerUrl = Configuration.GetValue<string>("MediaApiServer" + Constants.DebugKinaUnaServer);
            }

            services.Configure<AuthConfigurations>(config => { config.StsServer = authorityServerUrl; config.ProtectedApiUrl = progenyServerUrl + " " + mediaServerUrl;});
            
            services.AddCors(o =>
            {
                if (_env.IsDevelopment())
                {
                    o.AddDefaultPolicy(builder =>
                    {
                        builder.WithOrigins("https://*.kinauna.io", "https://nuuk2015.kinauna.io:44324",
                            "https://nuuk2020.kinauna.io:44324", "https://nuuk2015.kinauna.io:44397",
                            "https://nuuk2020.kinauna.io:44397", "https://nuuk2015.kinauna.io",
                            "https://nuuk2020.kinauna.io").SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    });
                }

                o.AddPolicy("KinaUnaCors", builder =>
                {
                    if (_env.IsDevelopment())
                    {
                        builder.WithOrigins("https://*.kinauna.io", "https://nuuk2015.kinauna.io:44324",
                                "https://nuuk2020.kinauna.io:44324", "https://nuuk2015.kinauna.io:44397",
                                "https://nuuk2020.kinauna.io:44397", "https://nuuk2015.kinauna.io",
                                "https://nuuk2020.kinauna.io").SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    }
                    else
                    {
                        builder.WithOrigins("https://*." + Constants.AppRootDomain)
                            .SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    }
                });
            });

            //services.AddLocalization(o =>
            //{
            //    o.ResourcesPath = "Resources";
            //});
            
            services.AddControllersWithViews(options =>
            {
                AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson().AddViewLocalization()
            //.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
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

            app.UseCors("KinaUnaCors");
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
