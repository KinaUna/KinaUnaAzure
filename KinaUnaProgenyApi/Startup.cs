using System;
using System.Reflection;
using IdentityServer4.AccessTokenValidation;
using KinaUna.Data.Contexts;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KinaUnaProgenyApi
{
    public class Startup(IConfiguration configuration)
    {
        private IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            string authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            string authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            string authenticationServerClientSecret = Configuration["AuthenticationServerClientSecret"];

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

            services.AddControllers().AddNewtonsoftJson();

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityServerUrl;
                    options.ApiName = authenticationServerClientId;
                    options.ApiSecret = authenticationServerClientSecret;
                    options.RequireHttpsMetadata = true;
                    options.EnableCaching = true;
                    options.CacheDuration = TimeSpan.FromSeconds(600);
                });
            services.AddAuthorization();
            services.AddApplicationInsightsTelemetry();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
