using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services;

public interface IRunTaskService
{
    Task CheckPictureExtensions(KinaUnaBackgroundTask task);
}