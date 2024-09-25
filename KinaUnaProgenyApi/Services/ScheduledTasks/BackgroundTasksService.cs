using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public class BackgroundTasksService(IServiceScopeFactory serviceScopeFactory) : IBackgroundTasksService
{
    public async Task<List<KinaUnaBackgroundTask>> GetTasks()
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
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
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        KinaUnaBackgroundTask task = await context.BackgroundTasksDb.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == taskId);

        return task;
    }

    public async Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
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
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        KinaUnaBackgroundTask existingTask = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        if (existingTask == null) return task;

        existingTask.TaskName = task.TaskName;
        existingTask.TaskDescription = task.TaskDescription;
        existingTask.ApiEndpoint = task.ApiEndpoint;
        existingTask.LastRun = task.LastRun;
        existingTask.Interval = task.Interval;
        existingTask.IsRunning = task.IsRunning;
        existingTask.IsEnabled = task.IsEnabled;

        context.Update(existingTask);
        _ = context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteTask(int taskId)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        KinaUnaBackgroundTask task = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null) return false;
        context.BackgroundTasksDb.Remove(task);
        _ = await context.SaveChangesAsync();

        return true;
    }

    public async Task ResetTasks()
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        List<KinaUnaBackgroundTask> tasks = await context.BackgroundTasksDb.ToListAsync();
        foreach (KinaUnaBackgroundTask task in tasks)
        {
            task.IsRunning = false;
            _ = await UpdateTask(task);
        }
    }
}
