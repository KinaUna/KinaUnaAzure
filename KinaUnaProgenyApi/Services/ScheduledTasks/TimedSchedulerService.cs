#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
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
                int tasksStarted = 0;
                int tasksRunning = 0;
                // Get a list of tasks to perform.
                CustomResult<List<KinaUnaBackgroundTask>> getTasksResult = await backgroundTasksService.GetTasks();
                if (getTasksResult.IsSuccess)
                {
                    // For each task, check if the task is due to be performed.
                    
                    foreach (KinaUnaBackgroundTask task in getTasksResult.Value)
                    {
                        if (!task.IsEnabled) continue;
                        if (task.IsRunning)
                        {
                            tasksRunning++;
                            continue;
                        }

                        if (DateTime.UtcNow - task.LastRun <= task.Interval) continue;

                        repeatingTasksService.RunRepeatingTask(task);
                        tasksStarted++;
                        tasksRunning++;
                    }
                }
                
                logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);
                logger.LogInformation("TimedSchedulerService tasks started count: {TasksStartedCount}", tasksStarted);
                logger.LogInformation("TimedSchedulerService tasks running count: {TasksRunningCount}", tasksRunning);
                logger.LogInformation("TimedSchedulerService tasks count: {TaskCount}", getTasksResult.Value.Count);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TimedSchedulerService: An error occurred.");
            }
        }
    }
}
