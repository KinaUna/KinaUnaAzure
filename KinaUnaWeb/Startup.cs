using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Utilities;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Server.AspNetCore;
using Quartz;
using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.Secure = CookieSecurePolicy.Always;
            });

            string authOpenIddictClientConnection = Configuration["AuthOpenIddictClientConnection"] ?? throw new InvalidOperationException("AuthOpenIddictClientConnection was not found in the configuration data.");
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(authOpenIddictClientConnection,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
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
            services.AddScoped<ApiTokenInMemoryClient>();
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
            string webServerClientId = Configuration.GetValue<string>("WebServerClientId");
            // string webServerApiClientId = Configuration.GetValue<string>("WebServerApiClientId");
            string authenticationServerClientSecret = Configuration.GetValue<string>("OpenIddictSecretString");
            string progenyServerUrl = Configuration.GetValue<string>("ProgenyApiServer"); 
            string progenyApiName = Constants.ProgenyApiName;
            // Todo: Configure these URLs for Azure client too.
            if (env.IsDevelopment())
            {
                authorityServerUrl = Configuration.GetValue<string>("AuthenticationServerLocal");
                webServerClientId = Configuration.GetValue<string>("WebServerClientIdLocal");
                // webServerApiClientId = Configuration.GetValue<string>("WebServerApiClientIdLocal");
                authenticationServerClientSecret = Configuration.GetValue<string>("OpenIddictSecretStringLocal");
                progenyServerUrl = Configuration.GetValue<string>("ProgenyApiServerLocal");
                progenyApiName = Constants.ProgenyApiName + "local";
            }
            // Register the OpenIddict services and configure them.
            string serverEncryptionCertificateThumbprint = Configuration["ServerEncryptionCertificateThumbprint"] ??
                                                           throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
            string serverSigningCertificateThumbprint = Configuration["ServerSigningCertificateThumbprint"] ??
                                                        throw new InvalidOperationException("ServerSigningCertificateThumbprint was not found in the configuration data.");
            X509Certificate2 encryptionCertificate = CertificateTools.GetCertificate(serverEncryptionCertificateThumbprint);
            X509Certificate2 signingCertificate = CertificateTools.GetCertificate(serverSigningCertificateThumbprint);

            services.AddHttpClient("oidc", client =>
            {
                client.BaseAddress = new Uri(authorityServerUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddScoped<ITokenService, TokenService>();

            services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
            }).AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
                options.SlidingExpiration = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
            });
            
            services.AddQuartz(options =>
            {
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
            services.AddOpenIddict()

                // Register the OpenIddict core components.
                .AddCore(options =>
                {
                    // Configure OpenIddict to use the Entity Framework Core stores and models.
                    // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                    options.UseEntityFrameworkCore()
                            .UseDbContext<ApplicationDbContext>();

                    // Developers who prefer using MongoDB can remove the previous lines
                    // and configure OpenIddict to use the specified MongoDB database:
                    // options.UseMongoDb()
                    //        .UseDatabase(new MongoClient().GetDatabase("openiddict"));

                    // Enable Quartz.NET integration.
                    options.UseQuartz();
                })

                // Register the OpenIddict client components.
                .AddClient(options =>
                {
                    options.AllowClientCredentialsFlow()
                        .AllowAuthorizationCodeFlow()
                        .AllowRefreshTokenFlow();

                    // Register the signing and encryption credentials used to protect
                    // sensitive data like the state tokens produced by OpenIddict.
                    options.AddEncryptionCertificate(encryptionCertificate);
                    options.AddSigningCertificate(signingCertificate);

                    // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                    options.UseAspNetCore()
                        .EnableStatusCodePagesIntegration()
                        .EnableRedirectionEndpointPassthrough()
                        .EnablePostLogoutRedirectionEndpointPassthrough();

                    // Register the System.Net.Http integration and use the identity of the current
                    // assembly as a more specific user agent, which can be useful when dealing with
                    // providers that use the user agent as a way to throttle requests (e.g Reddit).
                    options.UseSystemNetHttp()
                            .SetProductInformation(typeof(Startup).Assembly);

                    // Add a client registration matching the client application definition in the server project.
                    options.AddRegistration(new OpenIddictClientRegistration
                    {
                        Issuer = new Uri(authorityServerUrl, UriKind.Absolute),

                        ClientId = webServerClientId,
                        ClientSecret = authenticationServerClientSecret,
                        Scopes =
                        {
                            OpenIddictConstants.Scopes.Email,
                            OpenIddictConstants.Scopes.Profile,
                            OpenIddictConstants.Scopes.Roles,
                            OpenIddictConstants.Scopes.OfflineAccess,
                            progenyApiName
                        },

                        // Note: to mitigate mix-up attacks, it's recommended to use a unique redirection endpoint
                        // URI per provider, unless all the registered providers support returning a special "iss"
                        // parameter containing their URL as part of authorization responses. For more information,
                        // see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics#section-4.4.
                        RedirectUri = new Uri("callback/login/local", UriKind.Relative),
                        PostLogoutRedirectUri = new Uri("callback/logout/local", UriKind.Relative)
                    });
                })
                .AddValidation(options =>
                {
                    //options.AddEncryptionCertificate(encryptionCertificate);
                    //options.AddSigningCertificate(signingCertificate);
                    options.UseSystemNetHttp();
                    options.SetIssuer(authorityServerUrl);

                    //if (_env.IsDevelopment())
                    //{
                    //    options.AddAudiences("kinaunawebclientlocal", Constants.ProgenyApiName + "local");
                    //}
                    //else
                    //{
                    //    options.AddAudiences("kinaunawebclient", Constants.ProgenyApiName);
                    //}
                    options.UseAspNetCore();
                });

            services.AddHostedService<OpenIddictWorkerService>();

            services.Configure<AuthConfigurations>(config =>
            {
                config.StsServer = authorityServerUrl;
                config.ProtectedApiUrl = progenyServerUrl;
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
                // In production, only allow requests from the specified origin.
                // This is important for security reasons.
                services.AddCors(options => options.AddDefaultPolicy(policy =>
                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(Constants.ProductionCorsList)));
            }

            services.AddControllersWithViews(options =>
            {
                AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson().AddViewLocalization();

            IMvcBuilder mvcBuilder = services.AddRazorPages();

            if (env.IsDevelopment())
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
                        
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WebNotificationHub>("/webnotificationhub");
                endpoints.MapDefaultControllerRoute();
            });            
        }
    }
}
