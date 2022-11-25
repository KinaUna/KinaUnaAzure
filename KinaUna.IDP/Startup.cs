﻿using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using KinaUna.IDP.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.EntityFramework.Entities;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System.Threading.Tasks;

namespace KinaUna.IDP
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            _env = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _ = services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = delegate { return true; };
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.Secure = CookieSecurePolicy.Always;
            });

            string migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

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

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"],
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

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireDigit = false;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(90);
            });

            services.AddTransient<ILoginService<ApplicationUser>, EfLoginService>();
            services.AddTransient<IRedirectService, RedirectService>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<ILocaleManager, LocaleManager>();
            
            X509Certificate2 cert = null;
            using (X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    Configuration["X509ThumbPrint"] ?? throw new InvalidOperationException("X509ThumbPrint value not found in configuration."),
                    false);
                // Get the first cert with the thumbprint
                if (certCollection.Count > 0)
                {
                    cert = certCollection[0];
                }
            }

            if (_env.IsDevelopment())
            {
                services.AddCors(options =>
                {
                    if (string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
                    {
                        options.AddPolicy("KinaUnaCors",
                            builder =>
                            {
                                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                            });
                    }
                    else
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder.WithOrigins("https://*.kinauna.io", "https://nuuk2015.kinauna.io:44324", "https://nuuk2020.kinauna.io:44324", "https://nuuk2015.kinauna.io:44397", "https://nuuk2020.kinauna.io:44397", "https://nuuk2015.kinauna.io", "https://nuuk2020.kinauna.io")
                                .SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        });

                        options.AddPolicy("KinaUnaCors",
                            builder =>
                            {
                                builder.WithOrigins("https://*.kinauna.io", "https://nuuk2015.kinauna.io:44324", "https://nuuk2020.kinauna.io:44324", "https://nuuk2015.kinauna.io:44397", "https://nuuk2020.kinauna.io:44397", "https://nuuk2015.kinauna.io", "https://nuuk2020.kinauna.io")
                                    .SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                            });
                    }
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                        {
                            builder.WithOrigins(Constants.WebAppUrl, "https://" + Constants.AppRootDomain);
                        });
                    options.AddPolicy("KinaUnaCors",
                        builder =>
                        {
                            builder.WithOrigins("https://*." + Constants.AppRootDomain, "https://*.pivoq.at").SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        });
                    options.AddPolicy("PivoqCors", builder => { builder.WithOrigins("https://*.pivoq.at").SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
                });
            }

            services.AddDistributedMemoryCache();

            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            services.AddIdentityServer(x =>
                {
                    x.Authentication.CookieLifetime = TimeSpan.FromDays(90);
                    x.Authentication.CookieSlidingExpiration = true;
                    if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
                    {
                        x.IssuerUri =
                            Configuration.GetValue<string>("AuthenticationServer" + Constants.DebugKinaUnaServer);
                    }
                })
                .AddSigningCredential(cert)
                .AddAspNetIdentity<ApplicationUser>()
                // This adds the config data from DB (clients, resources)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration["AuthDefaultConnection"],
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // This adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration["AuthDefaultConnection"],
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    // This enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 3600;
                })
                .AddInMemoryCaching()
                .AddClientStoreCache<IdentityServer4.EntityFramework.Stores.ClientStore>()
                .AddResourceStoreCache<IdentityServer4.EntityFramework.Stores.ResourceStore>()
                .AddCorsPolicyCache<IdentityServer4.EntityFramework.Services.CorsPolicyService>()
                .AddProfileServiceCache<ProfileService>()
                .Services.AddTransient<IProfileService, ProfileService>();

            services.AddLocalApiAuthentication();

            services.AddAuthentication(o => { o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = "KinaUnaCookie";
                    options.SlidingExpiration = true;
                    options.Events.OnSigningIn = (context) =>
                    {
                        context.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(30);
                        context.CookieOptions.IsEssential = true;
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
                .AddApple(options =>
                {
                    options.ClientId = Configuration["AppleClientId"] ?? throw new InvalidOperationException("AppleClientId missing in configuration."); 
                    options.KeyId = Configuration["AppleKeyId"];
                    options.TeamId = Configuration["AppleTeamId"] ?? throw new InvalidOperationException("AppleTeamId missing in configuration");
                    options.UsePrivateKey((keyId) => _env.ContentRootFileProvider.GetFileInfo($"AuthKey_{keyId}.p8"));
                    options.SaveTokens = true;
                })
                .AddGoogle("Google", "Google", options =>
                {
                    options.ClientId = Configuration["GoogleClientId"] ?? throw new InvalidOperationException("GoogleClientId missing in configuration.");
                    options.ClientSecret = Configuration["GoogleClientSecret"] ?? throw new InvalidOperationException("GoogleClientSecret missing in configuration.");
                    options.SaveTokens = true;
                })
                .AddMicrosoftAccount("Microsoft", "Microsoft", microsoftOptions =>
                {
                    microsoftOptions.ClientId = Configuration["MicrosoftClientId"] ?? throw new InvalidOperationException("MicrosoftClientId missing in configuration.");
                    microsoftOptions.ClientSecret = Configuration["MicrosoftClientSecret"] ?? throw new InvalidOperationException("MicrosoftClientSecret missing in configuration");
                    microsoftOptions.SaveTokens = true;
                });

            //    .AddFacebook("Facebook", "Facebook", options =>
            //{
            //    options.ClientId = Configuration["FacebookClientId"];
            //    options.ClientSecret = Configuration["FacebookClientSecret"];
            //    options.SaveTokens = true;
            //})

            services.AddApplicationInsightsTelemetry();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment hostingEnvironment
            )
        {
            app.UseHttpsRedirection();
            // This will do the initial DB population
            InitializeDatabase(app, Constants.ResetIdentityDb);

            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
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
            app.UseCookiePolicy();
            app.UseRequestLocalization(localizationOptions);
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("KinaUnaCors");
            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void InitializeDatabase(IApplicationBuilder app, bool resetDb)
        {
            using (IServiceScope serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope())
            {
                if (serviceScope != null)
                {
                    serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                    ConfigurationDbContext context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                    context.Database.Migrate();

                    ProgenyDbContext usersContext = serviceScope.ServiceProvider.GetRequiredService<ProgenyDbContext>();

                    usersContext.Database.Migrate();

                    if (resetDb)
                    {
                        List<Client> contextClients = context.Clients.ToList();
                        foreach (Client clientToDelete in contextClients)
                        {
                            context.Clients.Remove(clientToDelete);
                        }

                        List<ApiResource> contextApis = context.ApiResources.ToList();
                        foreach (ApiResource apiToDelete in contextApis)
                        {
                            context.ApiResources.Remove(apiToDelete);
                        }

                        List<IdentityResource> contextIdentities = context.IdentityResources.ToList();
                        foreach (IdentityResource identityToDelete in contextIdentities)
                        {
                            context.IdentityResources.Remove(identityToDelete);
                        }

                        List<ApiScope> contextApiScopes = context.ApiScopes.ToList();
                        foreach (ApiScope apiScopeToDelete in contextApiScopes)
                        {
                            context.ApiScopes.Remove(apiScopeToDelete);
                        }

                        context.SaveChanges();
                    }

                    if (!context.Clients.Any())
                    {
                        foreach (IdentityServer4.Models.Client client in Config.GetClients(Configuration))
                        {
                            context.Clients.Add(client.ToEntity());
                        }

                        context.SaveChanges();
                    }

                    if (!context.IdentityResources.Any())
                    {
                        foreach (IdentityServer4.Models.IdentityResource resource in Config.GetIdentityResources())
                        {
                            context.IdentityResources.Add(resource.ToEntity());
                        }

                        context.SaveChanges();
                    }


                    if (!context.ApiResources.Any())
                    {
                        foreach (IdentityServer4.Models.ApiResource resource in Config.GetApiResources(Configuration))
                        {
                            context.ApiResources.Add(resource.ToEntity());
                        }

                        context.SaveChanges();
                    }

                    if (!context.ApiScopes.Any())
                    {
                        foreach (IdentityServer4.Models.ApiScope resource in Config.ApiScopes)
                        {
                            context.ApiScopes.Add(resource.ToEntity());
                        }

                        context.SaveChanges();
                    }

                    if (usersContext.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.DefaultUserEmail.ToUpper()) == null)
                    {
                        UserInfo userInfo = new UserInfo();
                        userInfo.UserEmail = Constants.DefaultUserEmail;
                        userInfo.FirstName = "System";
                        userInfo.LastName = "Default User";
                        userInfo.Timezone = Constants.DefaultTimezone;
                        userInfo.UserName = Constants.DefaultUserEmail;
                        userInfo.ViewChild = Constants.DefaultChildId;

                        usersContext.UserInfoDb.Add(userInfo);
                        usersContext.SaveChanges();
                    }

                    if (usersContext.ProgenyDb.SingleOrDefault(p => p.Id == Constants.DefaultChildId) == null)
                    {
                        Progeny progeny = new Progeny();
                        progeny.Admins = Constants.AdminEmail;
                        progeny.BirthDay = DateTime.UtcNow;
                        progeny.Name = Constants.AppName;
                        progeny.NickName = Constants.AppName;
                        progeny.PictureLink = Constants.ProfilePictureUrl;
                        progeny.TimeZone = Constants.DefaultTimezone;

                        usersContext.ProgenyDb.Add(progeny);
                        usersContext.SaveChanges();
                    }

                    if (usersContext.UserAccessDb.SingleOrDefault(u =>
                            u.ProgenyId == Constants.DefaultChildId &&
                            u.UserId.ToUpper() == Constants.DefaultUserEmail.ToUpper()) == null)
                    {
                        UserAccess userAccess = new UserAccess();
                        userAccess.ProgenyId = Constants.DefaultChildId;
                        userAccess.UserId = Constants.DefaultUserEmail.ToUpper();
                        userAccess.AccessLevel = (int)AccessLevel.Users;
                        userAccess.CanContribute = false;

                        usersContext.UserAccessDb.Add(userAccess);
                        usersContext.SaveChanges();
                    }

                    if (usersContext.UserAccessDb.SingleOrDefault(u =>
                            u.ProgenyId == Constants.DefaultChildId &&
                            u.UserId.ToUpper() == Constants.AdminEmail.ToUpper()) == null)
                    {
                        UserAccess userAccess = new UserAccess();
                        userAccess.ProgenyId = Constants.DefaultChildId;
                        userAccess.UserId = Constants.AdminEmail.ToUpper();
                        userAccess.AccessLevel = (int)AccessLevel.Private;
                        userAccess.CanContribute = true;

                        usersContext.UserAccessDb.Add(userAccess);
                        usersContext.SaveChanges();
                    }
                }
            }
        }
    }
}
