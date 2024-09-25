#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using KinaUna.Data.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


// Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio
namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public class TimedSchedulerService(IBackgroundTasksService backgroundTasksService, IRepeatingTasksService repeatingTasksService, ILogger<TimedSchedulerService> logger) : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);
    private int _executionCount;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private bool _tasksReset;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (DateTime.UtcNow - _startTime < TimeSpan.FromMinutes(1)) return; // Wait before starting tasks. To allow other services to start.
            
            try
            {
                if (!_tasksReset)
                {
                    _tasksReset = true;
                    await backgroundTasksService.ResetTasks();
                }

                int count = Interlocked.Increment(ref _executionCount);

                // Get a list of tasks to perform.
                List<KinaUnaBackgroundTask> tasks = await backgroundTasksService.GetTasks();
                // For each task, check if the task is due to be performed.
                int tasksStarted = 0;
                foreach (KinaUnaBackgroundTask task in tasks)
                {
                    if (DateTime.UtcNow - task.LastRun <= task.Interval) continue;
                    if (task.IsRunning || !task.IsEnabled) continue;
                    repeatingTasksService.RunRepeatingTask(task);
                    tasksStarted++;
                }

                logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);
                logger.LogInformation("TimedSchedulerService tasks count: {TaskCount}", tasks.Count);
                logger.LogInformation("TimedSchedulerService tasks started count: {TasksStartedCount}", tasksStarted);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TimedSchedulerService: An error occurred.");
            }
        }
    }
}
