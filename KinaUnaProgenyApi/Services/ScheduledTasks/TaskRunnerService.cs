#nullable enable
using System;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public class TaskRunnerService(IBackgroundTasksService backgroundTasksService, IServiceScopeFactory serviceScopeFactory, ILogger<TaskRunnerService> logger) : ITaskRunnerService
{
    public async Task<CustomResult<KinaUnaBackgroundTask>> CheckPictureExtensions(KinaUnaBackgroundTask task)
    {
        _ = await UpdateTaskBeforeRun(task);
        try
        {
            // Create a new scope to get the required service, and inject the service into the scope.
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
            IPicturesService picturesService = scope.ServiceProvider.GetRequiredService<IPicturesService>();

            await picturesService.CheckPicturesForExtensions();
        }
        catch (Exception e)
        {
            _ = await UpdateTaskAfterRun(task);
            return CustomResult<KinaUnaBackgroundTask>.ExceptionCaughtFailure(e, logger);
        }

        return await UpdateTaskAfterRun(task);
    }

    public async Task<CustomResult<KinaUnaBackgroundTask>> CheckPictureLinks(KinaUnaBackgroundTask task)
    {
        _ = await UpdateTaskBeforeRun(task);
        try
        {
            // Create a new scope to get the required service, and inject the service into the scope.
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
            IPicturesService picturesService = scope.ServiceProvider.GetRequiredService<IPicturesService>();

            await picturesService.CheckPictureLinks();
        }
        catch (Exception e)
        {
            _ = await UpdateTaskAfterRun(task);
            return CustomResult<KinaUnaBackgroundTask>.ExceptionCaughtFailure(e, logger);
        }

        return await UpdateTaskAfterRun(task);
    }

    public async Task<CustomResult<KinaUnaBackgroundTask>> CheckPicturePropertiesForNull(KinaUnaBackgroundTask task)
    {
        _ = await UpdateTaskBeforeRun(task);
        try
        {
            // Create a new scope to get the required service, and inject the service into the scope.
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
            IPicturesService picturesService = scope.ServiceProvider.GetRequiredService<IPicturesService>();

            await picturesService.CheckPicturePropertiesForNull();
        }
        catch (Exception e)
        {
            _ = await UpdateTaskAfterRun(task);
            return CustomResult<KinaUnaBackgroundTask>.ExceptionCaughtFailure(e, logger);
        }

        return await UpdateTaskAfterRun(task);
    }

    /// <summary>
    /// Sets the task to running and updates the LastRun time.
    /// </summary>
    /// <param name="backgroundTask">The Repeating task to update.</param>
    /// <returns>The updated RepeatingTask.</returns>
    private async Task<CustomResult<KinaUnaBackgroundTask>> UpdateTaskBeforeRun(KinaUnaBackgroundTask backgroundTask)
    {
        KinaUnaBackgroundTask? taskToRun = await backgroundTasksService.GetTask(backgroundTask.TaskId);
        if (taskToRun == null)
        {
            return CustomResult<KinaUnaBackgroundTask>.Failure(CustomError.NotFoundError($"UpdateTaskBeforeRun: Task with id {backgroundTask.TaskId} not found", logger));
        }

        backgroundTask.IsRunning = true;
        backgroundTask.LastRun = DateTime.UtcNow;
        return await backgroundTasksService.UpdateTask(backgroundTask) ?? backgroundTask;
    }

    /// <summary>
    /// Sets the task to not running and updates the LastRun time.
    /// </summary>
    /// <param name="backgroundTask">The Repeating task to update.</param>
    /// <returns>The updated RepeatingTask.</returns>
    private async Task<CustomResult<KinaUnaBackgroundTask>> UpdateTaskAfterRun(KinaUnaBackgroundTask backgroundTask)
    {
        KinaUnaBackgroundTask? taskToRun = await backgroundTasksService.GetTask(backgroundTask.TaskId);
        if (taskToRun == null)
        {
            return CustomResult<KinaUnaBackgroundTask>.Failure(CustomError.NotFoundError($"UpdateTaskAfterRun: Task with id {backgroundTask.TaskId} not found", logger));
        }

        backgroundTask.IsRunning = false;
        backgroundTask.LastRun = DateTime.UtcNow;
        return await backgroundTasksService.UpdateTask(backgroundTask) ?? backgroundTask;
    }
}