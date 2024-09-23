using System;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public class RunTaskService(IBackgroundTasksService backgroundTasksService, IPicturesService picturesService) : IRunTaskService
    {
        public async Task CheckPictureExtensions(KinaUnaBackgroundTask task)
        {
            KinaUnaBackgroundTask existingTask = await backgroundTasksService.GetTask(task.TaskId);
            existingTask.IsRunning = true;
            existingTask.LastRun = DateTime.UtcNow;
            KinaUnaBackgroundTask updatedTask = await backgroundTasksService.UpdateTask(existingTask);
            if (updatedTask == null)
            {
                return ;
            }

            await picturesService.CheckPicturesForExtensions();

            KinaUnaBackgroundTask completedTask = await backgroundTasksService.GetTask(task.TaskId);
            completedTask.IsRunning = false;
            
            _ = await backgroundTasksService.UpdateTask(completedTask);

        }
    }
}
