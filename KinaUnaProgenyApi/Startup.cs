using IdentityServer4.AccessTokenValidation;
using KinaUna.Data.Contexts;
using KinaUnaProgenyApi.Authorization;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KinaUnaProgenyApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            var authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = Configuration["AuthenticationServerClientSecret"];

            services.AddSingleton<ImageStore>();

            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"]));

            services.AddDistributedRedisCache(option =>
                option.Configuration = Configuration["RedisConnection"]);

            services.AddScoped<IDataService, DataService>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthorization(authorizationOptions =>
            {
                authorizationOptions.AddPolicy(
                    "MustBeAdmin",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        policyBuilder.AddRequirements(
                            new MustBeAdminRequirement());
                    });

            });

            services.AddScoped<IAuthorizationHandler, MustBeAdminHandler>();
            
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityServerUrl;
                    options.ApiName = authenticationServerClientId;
                    options.ApiSecret = authenticationServerClientSecret;
                    options.RequireHttpsMetadata = false;
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            
            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
