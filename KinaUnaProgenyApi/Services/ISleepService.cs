using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ISleepService
    {
        Task<Sleep> GetSleep(int id);
        Task<Sleep> AddSleep(Sleep sleep);
        Task<Sleep> UpdateSleep(Sleep sleep);
        Task<Sleep> DeleteSleep(Sleep sleep);
        Task<List<Sleep>> GetSleepList(int progenyId);
    }
}
