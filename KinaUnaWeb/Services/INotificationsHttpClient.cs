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
    Task<List<PushDevices>> GetPushDevicesListByUserId(string user);
    Task<PushDevices> GetPushDevice(PushDevices device);
    Task<WebNotification> AddWebNotification(WebNotification notification);
    Task<WebNotification> UpdateWebNotification(WebNotification notification);
    Task<WebNotification> RemoveWebNotification(WebNotification notification);
    Task<WebNotification> GetWebNotificationById(int id);
    Task<List<WebNotification>> GetUsersWebNotifications(string user);
}