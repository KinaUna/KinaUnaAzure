using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services;

public interface INotificationsHttpClient
{
    Task<List<PushDevices>> GetAllPushDevices(bool updateCache = false);
    Task<PushDevices> GetPushDeviceById(int id, bool updateCache = false);
    Task<PushDevices> AddPushDevice(PushDevices device);
    Task<PushDevices> RemovePushDevice(PushDevices device);
    Task<List<PushDevices>> GetPushDeviceByUserId(string user);
    Task<PushDevices> GetPushDevice(PushDevices device);
}