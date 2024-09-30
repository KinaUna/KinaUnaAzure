using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public interface IBackgroundTasksService
{
    Task<CustomResult<List<KinaUnaBackgroundTask>>> GetTasks();
    Task<CustomResult<KinaUnaBackgroundTask>> GetTask(int taskId);
    Task<CustomResult<KinaUnaBackgroundTask>> AddTask(KinaUnaBackgroundTask task);
    Task<CustomResult<KinaUnaBackgroundTask>> UpdateTask(KinaUnaBackgroundTask task);
    Task<CustomResult<KinaUnaBackgroundTask>> DeleteTask(int taskId);
    Task<CustomResult<List<KinaUnaBackgroundTask>>> ResetTasks();
}