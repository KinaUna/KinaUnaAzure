using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public interface ITaskRunnerService
{
    Task CheckPictureExtensions(KinaUnaBackgroundTask task);
    Task CheckPictureLinks(KinaUnaBackgroundTask task);
    Task CheckPicturePropertiesForNull(KinaUnaBackgroundTask task);
}