using System;
using IdentityServer4.AccessTokenValidation;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUnaMediaApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace KinaUnaMediaApi
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer" + Constants.DebugKinaUnaServer);
            }
            var authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = Configuration["AuthenticationServerClientSecret"];

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"], s => s.MigrationsAssembly("KinaUna.IDP")));

            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"], s => s.MigrationsAssembly("KinaUna.IDP")));

            //services.AddDistributedRedisCache(option => option.Configuration = Configuration["RedisConnection"]);
            services.AddDistributedMemoryCache();
            services.AddScoped<IDataService, DataService>();

            services.AddControllers().AddNewtonsoftJson();
            services.AddSingleton<ImageStore>();
            services.AddScoped<AzureNotifications>();
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
    
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
