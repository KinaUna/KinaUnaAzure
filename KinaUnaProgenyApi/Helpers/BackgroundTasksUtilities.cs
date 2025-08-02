using System;
using KinaUnaProgenyApi.Services.ScheduledTasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Helpers
{
    public static class BackgroundTasksUtilities
    {
        /// <summary>
        /// Validates a RepeatingTask object, by checking if it has a name and the method that the Command invokes exists.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>bool, true if the command is valid and the name isn't empty.</returns>
        public static bool ValidateBackgroundTask(this KinaUnaBackgroundTask task)
        {
            if (string.IsNullOrEmpty(task.TaskName) || string.IsNullOrEmpty(task.ApiEndpoint)) return false;

            List<MethodInfo> methods = GetTaskRunnerServiceMethods();
            return methods.Any(m => string.Equals(m.Name, task.ApiEndpoint, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a list of all methods in the ITaskRunner service interface.
        /// </summary>
        /// <returns></returns>
        public static List<MethodInfo> GetTaskRunnerServiceMethods()
        {
            // Use reflection to get a list of methods in the TaskRunner service class.
            return [.. typeof(ITaskRunnerService).GetMethods()];
        }

        /// <summary>
        /// Gets a list of all available commands.
        /// </summary>
        /// <returns>List of string with each command name.</returns>
        public static List<string> GetCommands()
        {
            List<MethodInfo> methods = GetTaskRunnerServiceMethods();
            return [.. methods.Select(m => m.Name)];
        }
    }
}
