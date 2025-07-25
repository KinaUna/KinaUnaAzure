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
            string authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            string progenyApiClientId = Configuration.GetValue<string>("ProgenyApiClient");
            //string webServerClientId = Configuration.GetValue<string>("WebServerClientId");
            //string webServerApiClientId = Configuration.GetValue<string>("WebServerApiClientId");
            //string authServerApiClientId = Configuration.GetValue<string>("AuthApiClientId");

            string authenticationServerClientSecret = Configuration.GetValue<string>("OpenIddictSecretString");
            
            if (env.IsDevelopment())
            {
                authorityServerUrl = Configuration.GetValue<string>("AuthenticationServerLocal");
                progenyApiClientId = Configuration.GetValue<string>("ProgenyApiClientLocal");
                //webServerClientId = Configuration.GetValue<string>("WebServerClientIdLocal");
                //webServerApiClientId = Configuration.GetValue<string>("WebServerApiClientIdLocal");
                //authServerApiClientId = Configuration.GetValue<string>("AuthApiClientIdLocal");
                authenticationServerClientSecret = Configuration.GetValue<string>("OpenIddictSecretStringLocal");
            }

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
                    options.UseIntrospection()
                        .SetClientId(progenyApiClientId)
                        .SetClientSecret(authenticationServerClientSecret);

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
                // In production, only allow requests from the specified origin.
                // This is important for security reasons.
                services.AddCors(options => options.AddDefaultPolicy(policy =>
                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(Constants.ProductionCorsList)));
            }

            services.AddAuthentication(options => { options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme; });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserOrClient", policy =>
                {
                    policy.Requirements.Add(new UserOrClientRequirement());
                });
            });

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
