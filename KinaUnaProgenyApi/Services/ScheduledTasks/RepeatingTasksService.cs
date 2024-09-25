#nullable enable
using System;
using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KinaUnaProgenyApi.Services.ScheduledTasks
{
    public class RepeatingTasksService(ITaskRunnerService taskRunnerService) : IRepeatingTasksService
    {
        public void RunRepeatingTask(KinaUnaBackgroundTask task)
        {
            // Use reflection to get a list of methods in the TaskRunner service class.
            List<MethodInfo> methods = GetTaskRunnerServiceMethods();

            // Check if the command matches a method name.
            MethodInfo? method = methods.FirstOrDefault(m => string.Equals(m.Name, task.ApiEndpoint, StringComparison.CurrentCultureIgnoreCase));

            // If it does, invoke the method, passing in the RepeatingTask object.
            _ = method?.Invoke(taskRunnerService, new object[] { task });
        }

        private List<MethodInfo> GetTaskRunnerServiceMethods()
        {
            return typeof(ITaskRunnerService).GetMethods().ToList();
        }

        public List<string> GetCommands()
        {
            List<MethodInfo> methods = GetTaskRunnerServiceMethods();
            return methods.Select(m => m.Name).ToList();
        }

        public bool ValidateRepeatingTask(KinaUnaBackgroundTask task)
        {
            if (string.IsNullOrEmpty(task.TaskName) || string.IsNullOrEmpty(task.ApiEndpoint)) return false;

            List<MethodInfo> methods = GetTaskRunnerServiceMethods();
            return methods.Any(m => string.Equals(m.Name, task.ApiEndpoint, StringComparison.OrdinalIgnoreCase));
        }
    }
    
}
