using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ITasksHttpClient
    {
        Task<KinaUnaBackgroundTask> AddTask(KinaUnaBackgroundTask task);
        Task<bool> DeleteTask(int taskId);
        Task<KinaUnaBackgroundTask> ExecuteTask(string apiEndpoint);
        Task<List<KinaUnaBackgroundTask>> GetTasks();
        Task<KinaUnaBackgroundTask> UpdateTask(KinaUnaBackgroundTask task);
    }
}