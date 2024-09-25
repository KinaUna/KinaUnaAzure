using System;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KinaUnaProgenyApi.Services.ScheduledTasks
{
    public class TaskRunnerService(IBackgroundTasksService backgroundTasksService, IServiceScopeFactory serviceScopeFactory, ILogger<TaskRunnerService> logger) : ITaskRunnerService
    {
        public async Task CheckPictureExtensions(KinaUnaBackgroundTask task)
        {
            KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
            existingTask.IsRunning = true;
            existingTask.LastRun = DateTime.UtcNow;
            KinaUnaBackgroundTask updatedTask = await backgroundTasksService.UpdateTask(existingTask);
            if (updatedTask == null)
            {
                return;
            }

            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IPicturesService picturesService = scope.ServiceProvider.GetRequiredService<IPicturesService>();

                await picturesService.CheckPicturesForExtensions();
            }
            catch (Exception e)
            {
                logger.LogError(e, "TaskRunnerService, CheckPictureExtensions: Error checking picture extensions.");
            }
            finally
            {
                KinaUnaBackgroundTask completedTask = await backgroundTasksService.GetTask(task.TaskId);
                completedTask.IsRunning = false;
                completedTask.LastRun = DateTime.UtcNow;
                _ = await backgroundTasksService.UpdateTask(completedTask);
            }
        }

        public async Task CheckPictureLinks(KinaUnaBackgroundTask task)
        {
            KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
            existingTask.IsRunning = true;
            existingTask.LastRun = DateTime.UtcNow;
            KinaUnaBackgroundTask updatedTask = await backgroundTasksService.UpdateTask(existingTask);
            if (updatedTask == null)
            {
                return;
            }

            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IPicturesService picturesService = scope.ServiceProvider.GetRequiredService<IPicturesService>();

                await picturesService.CheckPictureLinks();
            }
            catch (Exception e)
            {
                logger.LogError(e, "TaskRunnerService, CheckPictureLinks: Error checking picture links.");
            }
            finally
            {
                KinaUnaBackgroundTask completedTask = await backgroundTasksService.GetTask(task.TaskId);
                completedTask.IsRunning = false;
                completedTask.LastRun = DateTime.UtcNow;
                _ = await backgroundTasksService.UpdateTask(completedTask);
            }
        }

        public async Task CheckPicturePropertiesForNull(KinaUnaBackgroundTask task)
        {
            KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
            existingTask.IsRunning = true;
            existingTask.LastRun = DateTime.UtcNow;
            KinaUnaBackgroundTask updatedTask = await backgroundTasksService.UpdateTask(existingTask);
            if (updatedTask == null)
            {
                return;
            }

            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IPicturesService picturesService = scope.ServiceProvider.GetRequiredService<IPicturesService>();

                await picturesService.CheckPicturePropertiesForNull();
            }
            catch (Exception e)
            {
                logger.LogError(e, "TaskRunnerService, CheckPicturePropertiesForNull: Error checking picture properties for null.");
            }
            finally
            {
                KinaUnaBackgroundTask completedTask = await backgroundTasksService.GetTask(task.TaskId);
                completedTask.IsRunning = false;
                completedTask.LastRun = DateTime.UtcNow;
                _ = await backgroundTasksService.UpdateTask(completedTask);
            }
        }
    }
}
