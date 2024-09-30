#nullable enable
using System;
using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KinaUnaProgenyApi.Helpers;

namespace KinaUnaProgenyApi.Services.ScheduledTasks
{
    /// <summary>
    /// Service for running repeating tasks.
    /// </summary>
    /// <param name="taskRunnerService"></param>
    public class RepeatingTasksService(ITaskRunnerService taskRunnerService) : IRepeatingTasksService
    {
        public void RunRepeatingTask(KinaUnaBackgroundTask task)
        {
            // Use reflection to get a list of methods in the TaskRunner service class.
            List<MethodInfo> methods = BackgroundTasksUtilities.GetTaskRunnerServiceMethods();

            // Check if the command matches a method name.
            MethodInfo? method = methods.FirstOrDefault(m => string.Equals(m.Name, task.ApiEndpoint, StringComparison.CurrentCultureIgnoreCase));

            // If it does, invoke the method, passing in the RepeatingTask object.
            _ = method?.Invoke(taskRunnerService, new object[] { task });
        }
    }
    
}
