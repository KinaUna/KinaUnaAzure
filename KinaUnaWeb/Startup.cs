using IdentityModel;
using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using KinaUnaWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace KinaUnaWeb
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public static string WebRootPath { get; private set; }
        private IHostingEnvironment Env;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
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
                options.UseSqlServer(Configuration["WebDefaultConnection"]));

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(Configuration["DataProtectionConnection"]));

            services.AddSingleton<IXmlRepository, DataProtectionKeyRepository>();
            var built = services.BuildServiceProvider();
            services.AddDataProtection().AddKeyManagementOptions(options => options.XmlRepository = built.GetService<IXmlRepository>()).SetApplicationName("KinaUnaWebApp"); ;

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient();
            services.AddTransient<IProgenyManager, ProgenyManager>();
            services.AddHttpClient<IProgenyHttpClient, ProgenyHttpClient>();
            services.AddHttpClient<IMediaHttpClient, MediaHttpClient>();
            services.AddTransient<IIdentityParser<ApplicationUser>, IdentityParser>();
            services.AddSingleton<ImageStore>();
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<IPushMessageSender, PushMessageSender>();

            services.AddCors(o => o.AddPolicy("localCors", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }));

            services.AddCors(o => o.AddPolicy("KinaUnaCors", builder =>
            { // Todo: Update cors policy
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }));

            services.AddLocalization(o =>
            {
                o.ResourcesPath = "Resources";
            });

            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.AllowCombiningAuthorizeFilters = false;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            
            var authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            var authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = Configuration.GetValue<string>("AuthenticationServerClientSecret");

            
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(180);
                    
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
                    options.Scope.Add("kinaunaprogenyapi");
                    options.Scope.Add("kinaunamediaapi");
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
            services.AddKendo();
            services.AddSignalR().AddMessagePackProtocol();
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddAzureWebAppDiagnostics();
            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Trace);

            if (env.IsDevelopment())
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
                CookieName = "KinaUnaLanguage"
            };
            localizationOptions.RequestCultureProviders.Insert(0, provider);
            
            app.UseRequestLocalization(localizationOptions);

            app.UseFileServer();
            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    OnPrepareResponse = ctx =>
            //    {
            //        const int durationInSeconds = 60 * 60 * 24 * 30;
            //        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            //            "public,max-age=" + durationInSeconds;
            //    }
            //});

            app.UseAuthentication();

            var log = loggerFactory.CreateLogger("identity");

            app.UseSignalR(routes => routes.MapHub<WebNotificationHub>("/webnotificationhub"));
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
