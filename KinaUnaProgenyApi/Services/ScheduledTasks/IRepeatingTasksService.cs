using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public interface IRepeatingTasksService
{
    void RunRepeatingTask(KinaUnaBackgroundTask task);
}