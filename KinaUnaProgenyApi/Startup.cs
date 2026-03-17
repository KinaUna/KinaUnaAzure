using Azure.Storage.Blobs;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUnaProgenyApi.AuthorizationHandlers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data.Utilities;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using KinaUnaProgenyApi.Services.FamiliesServices;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.Search;
using KinaUnaProgenyApi.Services.TodosServices;

namespace KinaUnaProgenyApi
{
    public class Startup(IConfiguration configuration, IHostEnvironment env)
    {
        private IConfiguration Configuration { get; } = configuration;
        private IHostEnvironment Environment { get; } = env;

        public void ConfigureServices(IServiceCollection services)
        {
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

            IDataProtectionBuilder dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp");

            string? storageConnectionString = Configuration["BlobStorageConnectionString"];
            if (!string.IsNullOrWhiteSpace(storageConnectionString) && !Environment.IsEnvironment("Testing"))
            {
                dataProtectionBuilder.PersistKeysToAzureBlobStorage(storageConnectionString, "dataprotection", "kukeys.xml");
            }

            services.AddScoped<IImageStore, ImageStore>();
            services.AddScoped<INotificationsService, NotificationsService>();
            services.AddScoped<IProgenyService, ProgenyService>();
            services.AddScoped<IUserInfoService, UserInfoService>();
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
            services.AddScoped<IUserGroupsService, UserGroupsService>();
            services.AddScoped<IUserGroupAuditLogsService, UserGroupAuditLogsService>();
            services.AddScoped<IFamiliesService, FamiliesService>();
            services.AddScoped<IFamilyMembersService, FamilyMembersService>();
            services.AddScoped<IFamilyAuditLogsService, FamilyAuditLogsService>();
            services.AddScoped<IPermissionAuditLogsService, PermissionAuditLogsService>();
            services.AddScoped<IKinaUnaCacheService, KinaUnaInMemoryCacheService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddHostedService<TimedSchedulerService>();
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });


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

            // Check if PFX path and password are provided for Docker/Linux environments. If so, load the certificate from the PFX file; otherwise, load it from the certificate store.
            string certificatePfxPath = Configuration["CertificatePfxPath"];
            string certificatePfxPassword = Configuration["CertificatePfxPassword"];

            X509Certificate2 encryptionCertificate = !string.IsNullOrEmpty(certificatePfxPath)
                ? CertificateTools.GetCertificateFromPfxFile(certificatePfxPath, certificatePfxPassword)
                : CertificateTools.GetCertificate(serverEncryptionCertificateThumbprint);
            
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
            // Additional origins can be provided via the CorsOrigins configuration key (semicolon-separated).
            string[] configuredOrigins = (Configuration.GetValue<string>("CorsOrigins") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(origin => origin.Trim())
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // If development, allow any origin.
            if (env.IsDevelopment())
            {
                services.AddCors(options => options.AddDefaultPolicy(policy =>
                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins([.. Constants.DevelopmentCorsList, .. configuredOrigins])
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(15))));
            }
            // If production, restrict to the specified origin.
            else
            {
                if (env.IsStaging())
                {
                    services.AddCors(options => options.AddDefaultPolicy(policy =>
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithOrigins([.. Constants.StagingCorsList, .. configuredOrigins])
                            .SetPreflightMaxAge(TimeSpan.FromMinutes(15))));
                }
                else
                {
                    // In production, only allow requests from the specified origin.
                    // This is important for security reasons.
                    services.AddCors(options => options.AddDefaultPolicy(policy =>
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithOrigins([.. Constants.ProductionCorsList, .. configuredOrigins])));
                }

            }

            services.AddAuthentication(options => { options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme; });
            services.AddAuthorizationBuilder()
                .AddPolicy("UserOrClient", policy => { policy.Requirements.Add(new UserOrClientRequirement()); })
                .AddPolicy("Client", policy => { policy.Requirements.Add(new ClientRequirement()); }); 
            services.AddSingleton<IAuthorizationHandler, UserOrClientHandler>();
            services.AddSingleton<IAuthorizationHandler, ClientHandler>();
            services.AddApplicationInsightsTelemetry();
            services.AddHealthChecks();
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
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health").AllowAnonymous();
                endpoints.MapControllers();
            });
        }
    }
}
