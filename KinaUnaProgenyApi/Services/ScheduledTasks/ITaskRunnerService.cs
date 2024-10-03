using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Services.ScheduledTasks;

public interface ITaskRunnerService
{
    Task<CustomResult<KinaUnaBackgroundTask>> CheckPictureExtensions(KinaUnaBackgroundTask task);
    Task<CustomResult<KinaUnaBackgroundTask>> CheckPictureLinks(KinaUnaBackgroundTask task);
    Task<CustomResult<KinaUnaBackgroundTask>> CheckPicturePropertiesForNull(KinaUnaBackgroundTask task);
}