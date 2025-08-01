using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUnaProgenyApi.AuthorizationHandlers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data.Utilities;

namespace KinaUnaProgenyApi
{
    public class Startup(IConfiguration configuration, IHostEnvironment env)
    {
        private IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            TelemetryDebugWriter.IsTracingDisabled = true;
            
            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.OpenIddict");
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.OpenIddict");
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration["DataProtectionConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("KinaUna.OpenIddict");
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true;
                options.ServicesStopConcurrently = false;
            });

            services.AddDistributedMemoryCache();
            services.AddMemoryCache();
            services.AddScoped<IImageStore, ImageStore>();
            services.AddScoped<IAzureNotifications, AzureNotifications>();
            services.AddScoped<INotificationsService, NotificationsService>();
            services.AddScoped<IProgenyService, ProgenyService>();
            services.AddScoped<IUserInfoService, UserInfoService>();
            services.AddScoped<IUserAccessService, UserAccessService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<IFriendService, FriendService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<ITimelineService, TimelineService>();
            services.AddScoped<IMeasurementService, MeasurementService>();
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<ISkillService, SkillService>();
            services.AddScoped<ISleepService, SleepService>();
            services.AddScoped<IVaccinationService, VaccinationService>();
            services.AddScoped<IVocabularyService, VocabularyService>();
            services.AddScoped<ITextTranslationService, TextTranslationService>();
            services.AddScoped<IKinaUnaTextService, KinaUnaTextService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<IPicturesService, PicturesService>();
            services.AddScoped<IVideosService, VideosService>();
            services.AddScoped<ICommentsService, CommentsService>();
            services.AddScoped<IPushMessageSender, PushMessageSender>();
            services.AddScoped<IWebNotificationsService, WebNotificationsService>();
            services.AddScoped<ITimelineFilteringService, TimelineFilteringService>();
            services.AddSingleton<IBackgroundTasksService, BackgroundTasksService>();
            services.AddSingleton<ITaskRunnerService, TaskRunnerService>();
            services.AddSingleton<IRepeatingTasksService, RepeatingTasksService>();
            services.AddScoped<ICalendarRemindersService, CalendarRemindersService>();
            services.AddScoped<ICalendarRecurrencesService, CalendarRecurrencesService>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddHostedService<TimedSchedulerService>();
            services.AddControllers().AddNewtonsoftJson();
            
            
            // Register the OpenIddict services and configure them.
            string authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey) ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + " was not found in the configuration data.");
            string progenyApiClientId = Configuration.GetValue<string>(AuthConstants.ProgenyApiClientIdKey) ?? throw new InvalidOperationException(AuthConstants.ProgenyApiClientIdKey + " was not found in the configuration data.");
            //string webServerClientId = Configuration.GetValue<string>(AuthConstants.WebServerClientIdKey) ?? throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + " was not found in the configuration data.");
            //string webServerApiClientId = Configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey) ?? throw new InvalidOperationException(AuthConstants.WebServerApiClientIdKey + " was not found in the configuration data.");
            //string authServerApiClientId = Configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey) ?? throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + " was not found in the configuration data.");
            string authenticationServerClientSecret = Configuration.GetValue<string>(AuthConstants.ProgenyApiClientSecretKey) ?? throw new InvalidOperationException(AuthConstants.ProgenyApiClientSecretKey + " was not found in the configuration data.");

            if (env.IsDevelopment())
            {
                authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Local") ??
                                            throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Local was not found in the configuration data.");
                progenyApiClientId = Configuration.GetValue<string>(AuthConstants.ProgenyApiClientIdKey + "Local") ??
                                            throw new InvalidOperationException(AuthConstants.ProgenyApiClientIdKey + "Local was not found in the configuration data.");
                //webServerClientId = Configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Local") ??
                //                           throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + "Local was not found in the configuration data.");
                //webServerApiClientId = Configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey + "Local") ??
                //                              throw new InvalidOperationException(AuthConstants.WebServerApiClientIdKey + "Local was not found in the configuration data.");
                //authServerApiClientId = Configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Local") ??
                //                               throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Local was not found in the configuration data.");
                authenticationServerClientSecret = Configuration.GetValue<string>(AuthConstants.ProgenyApiClientSecretKey + "Local") ??
                                                          throw new InvalidOperationException(AuthConstants.ProgenyApiClientSecretKey + "Local was not found in the configuration data.");
            }

            if (env.IsStaging())
            {
                authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Azure") ??
                                     throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Azure was not found in the configuration data.");
                progenyApiClientId = Configuration.GetValue<string>(AuthConstants.ProgenyApiClientIdKey + "Azure") ??
                                     throw new InvalidOperationException(AuthConstants.ProgenyApiClientIdKey + "Azure was not found in the configuration data.");
                //webServerClientId = Configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Azure") ??
                //                    throw new InvalidOperationException(AuthConstants.WebServerClientIdKey + "Azure was not found in the configuration data.");
                //webServerApiClientId = Configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey + "Azure") ??
                //                       throw new InvalidOperationException(AuthConstants.WebServerApiClientIdKey + "Azure was not found in the configuration data.");
                //authServerApiClientId = Configuration.GetValue<string>(AuthConstants.AuthApiClientIdKey + "Azure") ??
                //                        throw new InvalidOperationException(AuthConstants.AuthApiClientIdKey + "Azure was not found in the configuration data.");
                authenticationServerClientSecret = Configuration.GetValue<string>(AuthConstants.ProgenyApiClientSecretKey + "Azure") ??
                                                   throw new InvalidOperationException(AuthConstants.ProgenyApiClientSecretKey + "Azure was not found in the configuration data.");
            }

            string serverEncryptionCertificateThumbprint = Configuration["ServerEncryptionCertificateThumbprint"]
                                                           ?? throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
            X509Certificate2 encryptionCertificate = CertificateTools.GetCertificate(serverEncryptionCertificateThumbprint);
            
            // Todo: Add Audience support.
            //string[] audienceList = [webServerClientId, webServerApiClientId, authServerApiClientId];
            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the address of the introspection endpoint.
                    options.SetIssuer(authorityServerUrl);
                    // options.AddAudiences(audienceList);

                    // Configure the validation handler to use introspection and register the client
                    // credentials used when communicating with the remote introspection endpoint.
                    //options.UseIntrospection()
                    //    .SetClientId(progenyApiClientId)
                    //    .SetClientSecret(authenticationServerClientSecret);

                    options.AddEncryptionCertificate(encryptionCertificate);


                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
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

            services.AddAuthentication(options => { options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme; });
            // services.AddScoped<ITokenValidationService, CachedTokenValidationService>();
            services.AddAuthorizationBuilder()
                .AddPolicy("UserOrClient", policy => { policy.Requirements.Add(new UserOrClientRequirement()); });

            services.AddSingleton<IAuthorizationHandler, UserOrClientHandler>();

            services.AddApplicationInsightsTelemetry();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
