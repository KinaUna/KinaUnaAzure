using IdentityServer4.AccessTokenValidation;
using KinaUna.Data.Contexts;
using KinaUnaMediaApi.Authorization;
using KinaUnaMediaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace KinaUnaMediaApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            var authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = Configuration["AuthenticationServerClientSecret"];

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"]));

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
            services.AddSingleton<ImageStore>();
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityServerUrl;
                    options.ApiName = authenticationServerClientId;
                    options.ApiSecret = authenticationServerClientSecret;
                    options.RequireHttpsMetadata = true;
                });
        }
    
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
