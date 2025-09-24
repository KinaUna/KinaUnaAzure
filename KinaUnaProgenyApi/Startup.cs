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
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamilyServices;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;

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
            services.AddScoped<ITodosService, TodosService>();
            services.AddScoped<ISubtasksService, SubtasksService>();
            services.AddScoped<IKanbanItemsService, KanbanItemsService>();
            services.AddScoped<IKanbanBoardsService, KanbanBoardsService>();
            services.AddScoped<IAccessManagementService, AccessManagementService>();
            services.AddScoped<IUserGroupService, UserGroupService>();
            services.AddScoped<IFamilyService, FamilyService>();
            services.AddScoped<IFamilyMembersService, FamilyMembersService>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddHostedService<TimedSchedulerService>();
            services.AddControllers().AddNewtonsoftJson();
            
            
            // Register the OpenIddict services and configure them.
            string authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey) ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + " was not found in the configuration data.");
            
            if (env.IsDevelopment())
            {
                authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Local") ??
                                            throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Local was not found in the configuration data.");
            }

            if (env.IsStaging())
            {
                authorityServerUrl = Configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Azure") ??
                                     throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Azure was not found in the configuration data.");
            }

            string serverEncryptionCertificateThumbprint = Configuration["ServerEncryptionCertificateThumbprint"]
                                                           ?? throw new InvalidOperationException("ServerEncryptionCertificateThumbprint was not found in the configuration data.");
            X509Certificate2 encryptionCertificate = CertificateTools.GetCertificate(serverEncryptionCertificateThumbprint);
            
            // Todo: Add Audience support.
            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    options.SetIssuer(authorityServerUrl);
                    options.AddEncryptionCertificate(encryptionCertificate);
                    options.UseSystemNetHttp(); 
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
            services.AddAuthorizationBuilder()
                .AddPolicy("UserOrClient", policy => { policy.Requirements.Add(new UserOrClientRequirement()); })
                .AddPolicy("Client", policy => { policy.Requirements.Add(new ClientRequirement()); }); 
            services.AddSingleton<IAuthorizationHandler, UserOrClientHandler>();
            services.AddSingleton<IAuthorizationHandler, ClientHandler>();
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
