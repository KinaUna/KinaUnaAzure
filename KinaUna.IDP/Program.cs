using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using KinaUna.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KinaUna.IDP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (!context.HostingEnvironment.IsProduction()) return;

                    const string keyVaultEndpoint = Constants.KeyVaultEndPoint;
                    if (string.IsNullOrEmpty(keyVaultEndpoint)) return;

                    config.Build();

                    config.AddAzureKeyVault(
                        new Uri(keyVaultEndpoint),
                        new DefaultAzureCredential(),
                        new AzureKeyVaultConfigurationOptions()
                        {
                            Manager = new KeyVaultSecretManager(),
                            ReloadInterval = TimeSpan.FromSeconds(15)
                        }
                    );
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
