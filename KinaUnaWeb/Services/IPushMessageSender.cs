using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface IPushMessageSender
    {
        Task SendMessage(string user, string title, string message, string link, string tag);
        Task<PushDevices> GetPushDeviceById(int id);

        Task<List<PushDevices>> GetAllPushDevices();
        Task<PushDevices> AddPushDevice(PushDevices device);
        Task<PushDevices> GetDevice(PushDevices device);
        Task RemoveDevice(PushDevices device);
    }
}
