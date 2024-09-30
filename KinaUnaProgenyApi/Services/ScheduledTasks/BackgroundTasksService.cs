﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public class BackgroundTasksService(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundTasksService> logger) : IBackgroundTasksService
{
    public async Task<CustomResult<List<KinaUnaBackgroundTask>>> GetTasks()
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

    public async Task<CustomResult<KinaUnaBackgroundTask>> GetTask(int taskId)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        KinaUnaBackgroundTask task = await context.BackgroundTasksDb.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null)
        {
            return CustomResult<KinaUnaBackgroundTask>.Failure(CustomError.NotFoundError($"GetTask: Task with id {taskId} not found.", logger));
        }

        return task;
    }

    public async Task<CustomResult<KinaUnaBackgroundTask>> AddTask(KinaUnaBackgroundTask task)
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
            return CustomResult<KinaUnaBackgroundTask>.Failure(CustomError.ValidationError("AddTask: Task already exists.", logger));
        }

        return task;
    }

    public async Task<CustomResult<KinaUnaBackgroundTask>> UpdateTask(KinaUnaBackgroundTask task)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        KinaUnaBackgroundTask existingTask = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        if (existingTask == null) return CustomResult<KinaUnaBackgroundTask>.Failure(CustomError.NotFoundError($"UpdateTask: Task with id {task.TaskId} not found", logger));

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

    public async Task<CustomResult<KinaUnaBackgroundTask>> DeleteTask(int taskId)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        KinaUnaBackgroundTask task = await context.BackgroundTasksDb.FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null) return CustomResult<KinaUnaBackgroundTask>.Failure(CustomError.NotFoundError($"DeleteTask: Task with id {taskId} not found", logger));
        context.BackgroundTasksDb.Remove(task);
        _ = await context.SaveChangesAsync();

        return task;
    }

    public async Task<CustomResult<List<KinaUnaBackgroundTask>>> ResetTasks()
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ProgenyDbContext context = scope.ServiceProvider.GetRequiredService<ProgenyDbContext>();
        List<KinaUnaBackgroundTask> tasks = await context.BackgroundTasksDb.ToListAsync();
        foreach (KinaUnaBackgroundTask task in tasks)
        {
            task.IsRunning = false;
            _ = await UpdateTask(task);
        }

        return tasks;
    }
}
