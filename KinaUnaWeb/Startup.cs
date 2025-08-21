using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace KinaUnaWeb
{
    public class Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        private IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            TelemetryDebugWriter.IsTracingDisabled = true;

            _ = services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = delegate { return true; };
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
            });

            string storageConnectionString = Configuration["BlobStorageConnectionString"];
            new BlobContainerClient(storageConnectionString, "dataprotection").CreateIfNotExists();

            services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp")
                .PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");
            services.AddDistributedMemoryCache();
            services.AddMemoryCache();
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
            services.AddTransient<ITodoItemsHttpClient, TodoItemsHttpClient>();
            services.AddTransient<ISubtasksHttpClient, SubtasksHttpClient>();
            services.AddHttpClient<IAutoSuggestsHttpClient, AutoSuggestsHttpClient>();
            services.AddSingleton<ITokenService, TokenService>();

            string authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey) ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + " was not found in the configuration data.");
            string webServerClientId = Configuration.GetValue<string>(AuthConstants.WebServerClientIdKey) ?? throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + " was not found in the configuration data.");
            string webServerUrl = Configuration.GetValue<string>(AuthConstants.WebServerUrlKey) ?? throw new InvalidOperationException(AuthConstants.WebServerUrlKey + " was not found in the configuration data.");
            string authenticationServerClientSecret = Configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey) ?? throw new InvalidOperationException(AuthConstants.WebServerClientSecretKey + " was not found in the configuration data.");
            string progenyApiName = AuthConstants.ProgenyApiName;
            string authApiName = AuthConstants.AuthApiName;

            if (env.IsDevelopment())
            {
                authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Local") ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Local was not found in the configuration data.");
                webServerClientId = Configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Local") ?? throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + "Local was not found in the configuration data.");
                webServerUrl = Configuration.GetValue<string>(AuthConstants.WebServerUrlKey + "Local") ?? throw new InvalidOperationException(AuthConstants.WebServerUrlKey + "Local was not found in the configuration data.");
                authenticationServerClientSecret = Configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey + "Local") ?? throw new InvalidOperationException(AuthConstants.WebServerClientSecretKey + "Local was not found in the configuration data.");
                progenyApiName = AuthConstants.ProgenyApiName + "local";
                authApiName = AuthConstants.AuthApiName + "local";
            }

            if (env.IsStaging())
            {
                authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Azure") ??
                                     throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Azure was not found in the configuration data.");
                webServerClientId = Configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Azure") ??
                                    throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + "Azure was not found in the configuration data.");
                webServerUrl = Configuration.GetValue<string>(AuthConstants.WebServerUrlKey + "Azure") ??
                               throw new InvalidOperationException(AuthConstants.WebServerUrlKey + "Azure was not found in the configuration data.");
                authenticationServerClientSecret = Configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey + "Azure") ??
                                                   throw new InvalidOperationException(AuthConstants.WebServerClientSecretKey + "Azure was not found in the configuration data.");
                progenyApiName = AuthConstants.ProgenyApiName + "azure";
                authApiName = AuthConstants.AuthApiName + "azure";
            }
            
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = ".kinauna.web.auth";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
            }).AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = authorityServerUrl; // OpenIddict server
                options.ClientId = webServerClientId;
                options.ClientSecret = authenticationServerClientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;

                options.Scope.Clear();
                options.Scope.Add(progenyApiName);
                options.Scope.Add(authApiName);
                options.Scope.Add("profile");
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("roles");
                options.Scope.Add(OpenIddictConstants.Scopes.OfflineAccess);

                options.ClaimActions.Remove("amr");
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false; // Prevents IdentityModel from mapping claims automatically, we want to use the original claim types from IdentityServer
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = OpenIddictConstants.Claims.Email,
                    RoleClaimType = OpenIddictConstants.Claims.Role
                };

                options.CallbackPath = "/callback/login";
                options.SignedOutCallbackPath = "/callback/logout";

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        ClaimsPrincipal claimsPrincipal = ctx.Principal;

                        // Clone and keep the existing authentication properties
                        AuthenticationProperties authProperties = ctx.Properties ?? new AuthenticationProperties();

                        // Make the cookie persistent
                        authProperties.IsPersistent = true;
                        authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(180);

                        // Manually sign in using the OIDC principal and the same properties
                        if (claimsPrincipal != null)
                            await ctx.HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                claimsPrincipal,
                                authProperties);

                        // Do NOT call ctx.HandleResponse()
                        // Let the middleware complete the normal flow (including token saving)
                    },
                    //Handle redirect to the Identity Provider(optional, but good practice)
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.RedirectUri = webServerUrl + "/callback/login";
                        return Task.CompletedTask;
                    }
                };
            });
            
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.Name = ".KinaUna.Auth";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(180);
                options.SlidingExpiration = true;
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // Configure CORS to allow requests from the specified origin.
            // If development, allow any origin.
            if (env.IsDevelopment())
            {
                services.AddCors(options => options.AddDefaultPolicy(policy =>
                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(Constants.DevelopmentCorsList)));
            }
            // If production, restrict to the specified origin.
            else
            {
                if (env.IsStaging())
                {
                    services.AddCors(options => options.AddDefaultPolicy(policy =>
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithOrigins(Constants.StagingCorsList)));
                }
                else
                {
                    // In production, only allow requests from the specified origin.
                    // This is important for security reasons.
                    services.AddCors(options => options.AddDefaultPolicy(policy =>
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithOrigins(Constants.ProductionCorsList)));
                }
                
            }
            
            services.AddAuthorization();

            services.AddControllersWithViews(options =>
            {
                AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson().AddViewLocalization();
            
            services.AddExceptionHandler<AuthenticationExceptionHandler>();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

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

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseExceptionHandler();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WebNotificationHub>("/webnotificationhub");
                endpoints.MapDefaultControllerRoute();
            });            
        }
    }
}
