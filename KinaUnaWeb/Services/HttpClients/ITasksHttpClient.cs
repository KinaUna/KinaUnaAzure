using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ITasksHttpClient
    {
        Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task);
        Task<bool> DeleteTask(int taskId);
        Task<KinaUnaBackgroundTask> ExecuteTask(KinaUnaBackgroundTask task);
        Task<List<KinaUnaBackgroundTask>> GetTasks();
        Task<List<KinaUnaBackgroundTask>> ResetTasks();
        Task<KinaUnaBackgroundTask> UpdateTask(KinaUnaBackgroundTask task);
    }
}