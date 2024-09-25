using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public interface IBackgroundTasksService
{
    Task<List<KinaUnaBackgroundTask>> GetTasks();
    Task<KinaUnaBackgroundTask> GetTask(int taskId);
    Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task);
    Task<KinaUnaBackgroundTask> UpdateTask(KinaUnaBackgroundTask task);
    Task<bool> DeleteTask(int taskId);
    Task ResetTasks();
}