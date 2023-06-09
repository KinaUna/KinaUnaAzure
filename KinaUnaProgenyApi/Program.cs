﻿using KinaUna.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;

namespace KinaUnaProgenyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // CreateWebHostBuilder(args).Build().Run();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (context.HostingEnvironment.IsProduction())
                    {
                        string keyVaultEndpoint = Constants.KeyVaultEndPoint;
                        if (!string.IsNullOrEmpty(keyVaultEndpoint))
                        {
                            config.Build();

                            AzureServiceTokenProvider azureServiceTokenProvider = new();
                            KeyVaultClient keyVaultClient = new(
                                new KeyVaultClient.AuthenticationCallback(
                                    azureServiceTokenProvider.KeyVaultTokenCallback));

                            config.AddAzureKeyVault(
                                keyVaultEndpoint,
                                keyVaultClient,
                                new DefaultKeyVaultSecretManager());
                        }
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
