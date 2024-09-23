#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio
namespace KinaUnaWeb.Services;

public class TimedSchedulerService(ITasksHttpClient tasksHttpClient, ILogger<TimedSchedulerService> logger) : IHostedService, IDisposable
{
    private int _executionCount;
    private Timer? _timer;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service running.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        _ = DoWorkAsync();
    }

    private async Task DoWorkAsync()
    {
        int count = Interlocked.Increment(ref _executionCount);
        // Get a list of tasks to perform.
        List<KinaUnaBackgroundTask> tasks = await tasksHttpClient.GetTasks(); // Todo: Get tasks from API/database.
        // For each task, check if the task is due to be performed.
        foreach (KinaUnaBackgroundTask task in tasks)
        {
            if (DateTime.UtcNow - task.LastRun > task.Interval)
            {
                if (task.IsRunning) continue; 
                await tasksHttpClient.ExecuteTask(task.ApiEndpoint);
            }
        }

        logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
