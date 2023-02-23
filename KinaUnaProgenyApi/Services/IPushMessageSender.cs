using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services;

public interface IPushMessageSender
{
    Task SendMessage(string user, string title, string message, string link, string tag);
    Task<PushDevices> GetPushDeviceById(int id);
    Task<List<PushDevices>> GetAllPushDevices();
    Task<PushDevices> AddPushDevice(PushDevices device);
    Task<PushDevices> GetDevice(PushDevices device);
    Task RemoveDevice(PushDevices device);
}