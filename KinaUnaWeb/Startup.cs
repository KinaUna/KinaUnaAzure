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
using Microsoft.AspNetCore.SignalR;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using KinaUna.Data;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using IdentityModel.Client;

namespace KinaUnaWeb
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public static string WebRootPath { get; private set; }
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
            WebRootPath = env.WebRootPath;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                // Todo: Fix consent to work with my cookies and language selection
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });

            services.AddDbContext<WebDbContext>(options =>
                options.UseSqlServer(Configuration["WebDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
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

            // services.AddSingleton<IXmlRepository, DataProtectionKeyRepository>();
            // var built = services.BuildServiceProvider();
            // services.AddDataProtection().AddKeyManagementOptions(options => options.XmlRepository = built.GetService<IXmlRepository>()).SetApplicationName("KinaUnaWebApp");

            var credentials = new StorageCredentials(Constants.CloudBlobUsername, Configuration["BlobStorageKey"]);
            CloudBlobClient blobClient = new CloudBlobClient(new Uri(Constants.CloudBlobBase), credentials);
            CloudBlobContainer container = blobClient.GetContainerReference("dataprotection");

            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp")
                .PersistKeysToAzureBlobStorage(container, "kukeys.xml");

            var authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            var authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = Configuration.GetValue<string>("AuthenticationServerClientSecret");

            //services.AddSingleton(new ClientCredentialsTokenRequest
            //{
            //    Address = authorityServerUrl + "/connect/token",
            //    ClientId = authenticationServerClientId,
            //    ClientSecret = authenticationServerClientSecret,
            //    Scope = Constants.ProgenyApiName + " " + Constants.MediaApiName
            //});

            //services.AddHttpClient<IIdentityServerClient, IdentityServerClient>(client =>
            //{
            //    client.BaseAddress = new Uri(authorityServerUrl);
            //    client.DefaultRequestHeaders.Add("Accept", "application/json");
            //});

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
            services.AddSingleton<ApiTokenInMemoryClient>();

            services.Configure<AuthConfigurations>(config => { config.StsServer = authorityServerUrl; config.ProtectedApiUrl = Configuration.GetValue<string>("ProgenyApiServer") + " " + Configuration.GetValue<string>("MediaApiServer"); });


            services.AddCors(o => o.AddPolicy("localCors", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

            services.AddCors(o => o.AddPolicy("KinaUnaCors", builder =>
            { // Todo: Update cors policy
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

            services.AddLocalization(o =>
            {
                o.ResourcesPath = "Resources";
            });
            
            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
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
                    options.ResponseType = "code id_token";
                    options.RequireHttpsMetadata = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("address");
                    options.Scope.Add("phone");
                    options.Scope.Add("roles");
                    options.Scope.Add("timezone");
                    options.Scope.Add("firstname");
                    options.Scope.Add("middlename");
                    options.Scope.Add("lastname");
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
                    options.ClaimActions.MapUniqueJsonKey("firstname", "firstname");
                    options.ClaimActions.MapUniqueJsonKey("middlename", "middlename");
                    options.ClaimActions.MapUniqueJsonKey("lastname", "lastname");
                    options.ClaimActions.MapUniqueJsonKey("phone_number", "phone_number");
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
            app.UseHttpsRedirection();
            app.UseCookiePolicy();
            var supportedCultures = new[]
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

            var provider = new CookieRequestCultureProvider()
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
                app.UseCors("localCors");
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseCors("KinaUnaCors");
            }

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
