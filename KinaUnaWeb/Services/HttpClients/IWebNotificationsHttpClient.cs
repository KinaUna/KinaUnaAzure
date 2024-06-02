using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients;

public interface IWebNotificationsHttpClient
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
    Task<List<WebNotification>> GetLatestWebNotifications(string user, int start = 0, int count = 10, bool unreadOnly = true);
    Task<int> GetUsersNotificationsCount(string userId);
}