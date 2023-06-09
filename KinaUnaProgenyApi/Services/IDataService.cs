﻿using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IDataService
    {
        Task<MobileNotification> GetMobileNotification(int id);
        Task<MobileNotification> AddMobileNotification(MobileNotification notification);
        Task<MobileNotification> UpdateMobileNotification(MobileNotification notification);
        Task<MobileNotification> DeleteMobileNotification(MobileNotification notification);
        Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language);
        Task<PushDevices> AddPushDevice(PushDevices device);
        Task RemovePushDevice(PushDevices device);
        Task<PushDevices> GetPushDeviceById(int id);
        Task<List<PushDevices>> GetAllPushDevices();
        Task<PushDevices> GetPushDevice(PushDevices device);
        Task<List<PushDevices>> GetPushDevicesListByUserId(string userId);
        Task<WebNotification> AddWebNotification(WebNotification notification);
        Task<WebNotification> UpdateWebNotification(WebNotification notification);
        Task RemoveWebNotification(WebNotification notification);
        Task<WebNotification> GetWebNotificationById(int id);
        Task<List<WebNotification>> GetUsersWebNotifications(string userId);
    }
}
