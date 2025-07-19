using KinaUna.Data.Contexts;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace KinaUnaWeb.Services
{
    public class OpenIddictWorkerService(IServiceProvider serviceProvider) : IHostedService
    {
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
