using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services;

public class BackgroundTasksService(ProgenyDbContext context) : IBackgroundTasksService
{
    public async Task<List<KinaUnaBackgroundTask>> GetTasks()
    {
        List<KinaUnaBackgroundTask> tasks = await context.BackgroundTasksDb.AsNoTracking().ToListAsync();

        if (tasks.Count != 0) return tasks;
        KinaUnaBackgroundTask task = new KinaUnaBackgroundTask
        {
            TaskName = "Check Picture Extensions",
            TaskDescription = "Checks all pictures for file extensions, if a picture has no extension it will be added and the picture updated.",
            ApiEndpoint = "CheckPictureExtensions",
            LastRun = new DateTime(2000, 1, 1),
            Interval = new TimeSpan(6, 0, 0),
            IsRunning = false,
            IsEnabled = true
        };
        _ = context.BackgroundTasksDb.Add(task);
        _ = await context.SaveChangesAsync();
            
        tasks.Add(task);
        return tasks;
    }

    public async Task<KinaUnaBackgroundTask> GetTask(int taskId)
    {
        KinaUnaBackgroundTask task = await context.BackgroundTasksDb.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == taskId);
            
        return task;
    }

    public async Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task)
    {
        KinaUnaBackgroundTask existingTask = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        if (existingTask == null)
        {
            context.BackgroundTasksDb.Add(task);
            _ = await context.SaveChangesAsync();
        }
        else
        {
            task = existingTask;
        }

        return task;
    }

    public async Task<KinaUnaBackgroundTask> UpdateTask(KinaUnaBackgroundTask task)
    {
        KinaUnaBackgroundTask existingTask = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        if (existingTask == null) return task;

        existingTask.TaskName = task.TaskName;
        existingTask.TaskDescription = task.TaskDescription;
        existingTask.ApiEndpoint = task.ApiEndpoint;
        existingTask.LastRun = task.LastRun;
        existingTask.Interval = task.Interval;
        existingTask.IsRunning = task.IsRunning;
        _ = context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteTask(int taskId)
    {
        KinaUnaBackgroundTask task = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null) return false;
        context.BackgroundTasksDb.Remove(task);
        _ = await context.SaveChangesAsync();

        return true;
    }
}
