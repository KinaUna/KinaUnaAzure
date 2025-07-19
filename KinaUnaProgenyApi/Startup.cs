using System;
using System.Reflection;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;

namespace KinaUnaProgenyApi
{
    public class Startup(IConfiguration configuration, IHostEnvironment env)
    {
        private IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            TelemetryDebugWriter.IsTracingDisabled = true;

            string authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            string authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            string authenticationServerClientSecret = Configuration.GetValue<string>("OpenIddictSecretString");
            
            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"], s => s.MigrationsAssembly("KinaUna.IDP")));

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"], s => s.MigrationsAssembly("KinaUna.IDP")));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration["DataProtectionConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
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

            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the address of the introspection endpoint.
                    options.SetIssuer(authorityServerUrl);
                    options.AddAudiences(Constants.ProgenyApiName);

                    // Configure the validation handler to use introspection and register the client
                    // credentials used when communicating with the remote introspection endpoint.
                    options.UseIntrospection()
                        .SetClientId(authenticationServerClientId)
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

            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            services.AddAuthorization();
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
