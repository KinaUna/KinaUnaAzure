using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public interface IRepeatingTasksService
{
    void RunRepeatingTask(KinaUnaBackgroundTask task);
    List<string> GetCommands();
    bool ValidateRepeatingTask(KinaUnaBackgroundTask task);
}