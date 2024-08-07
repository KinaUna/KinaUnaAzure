﻿using System;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using KinaUna.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KinaUna.IDP.Data
{
    public static class IdentityServerDatabaseInitialization
    {
        public static void InitializeDatabase(IServiceProvider services, IConfiguration configuration)
        {
            PerformMigrations(services);
            SeedData(services, configuration);

        }

        private static void PerformMigrations(IServiceProvider services)
        {
            services
                .GetRequiredService<ApplicationDbContext>()
                .Database.Migrate();
            services
                .GetRequiredService<ConfigurationDbContext>()
                .Database.Migrate();
            services
                .GetRequiredService<PersistedGrantDbContext>()
                .Database.Migrate();
        }

        private static void SeedData(IServiceProvider services, IConfiguration configuration)
        {
            ConfigurationDbContext context = services.GetRequiredService<ConfigurationDbContext>();

            if (!context.Clients.Any())
            {
                foreach (Client client in Config.GetClients(configuration))
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (IdentityResource resource in Config.GetIdentityResources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            //if (!context.ApiResources.Any())
            //{
            //    foreach (var resource in Config.GetApiResources(configuration))
            //    {
            //        context.ApiResources.Add(resource.ToEntity());
            //    }
            //    context.SaveChanges();
            //}

            if (context.ApiScopes.Any()) return;

            foreach (ApiScope resource in Config.ApiScopes)
            {
                context.ApiScopes.Add(resource.ToEntity());
            }
            context.SaveChanges();
            
        }
    }
}
