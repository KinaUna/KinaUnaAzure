using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class DataService(ProgenyDbContext progenyDbContext, WebDbContext webDbContext) : IDataService
    {
        public async Task<MobileNotification> GetMobileNotification(int id)
        {
            MobileNotification notification = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(m => m.NotificationId == id);
            return notification;
        }

        public async Task<MobileNotification> AddMobileNotification(MobileNotification notification)
        {
            _ = progenyDbContext.MobileNotificationsDb.Add(notification);
            _ = await progenyDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task<MobileNotification> UpdateMobileNotification(MobileNotification notification)
        {
            MobileNotification updatedNotification = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (updatedNotification != null)
            {
                updatedNotification.IconLink = notification.IconLink;
                updatedNotification.Title = notification.Title;
                updatedNotification.ItemId = notification.ItemId;
                updatedNotification.ItemType = notification.ItemType;
                updatedNotification.Language = notification.Language;
                updatedNotification.Message = notification.Message;
                updatedNotification.Read = notification.Read;
                updatedNotification.Time = notification.Time;
                updatedNotification.UserId = notification.UserId;

                _ = progenyDbContext.MobileNotificationsDb.Update(updatedNotification);
                _ = await progenyDbContext.SaveChangesAsync();
            }

            return notification;
        }

        public async Task<MobileNotification> DeleteMobileNotification(MobileNotification notification)
        {
            MobileNotification notificationToDelete = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (notificationToDelete != null)
            {
                _ = progenyDbContext.MobileNotificationsDb.Remove(notificationToDelete);
                _ = await progenyDbContext.SaveChangesAsync();
            }

            return notification;
        }
        public async Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language)
        {
            List<MobileNotification> notifications = await progenyDbContext.MobileNotificationsDb.Where(n => n.UserId == userId && n.Language.ToUpper() == language.ToUpper()).ToListAsync();
            if (string.IsNullOrEmpty(language))
            {
                notifications = await progenyDbContext.MobileNotificationsDb.Where(n => n.UserId == userId).ToListAsync();
            }

            return notifications;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            PushDevices existingDevice = await GetPushDevice(device);
            if (existingDevice != null)
            {
                return existingDevice;
            }

            webDbContext.PushDevices.Add(device);
            await webDbContext.SaveChangesAsync();

            return device;
        }

        public async Task RemovePushDevice(PushDevices device)
        {
            PushDevices existingDevice = await GetPushDevice(device);
            if (existingDevice == null)
            {
                return;
            }

            webDbContext.PushDevices.Remove(device);
            await webDbContext.SaveChangesAsync();
        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await webDbContext.PushDevices.SingleOrDefaultAsync(p => p.Id == id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            var pushDevicesList = await webDbContext.PushDevices.ToListAsync();

            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices result = await webDbContext.PushDevices.SingleOrDefaultAsync(p =>
                p.Name == device.Name && p.PushP256DH == device.PushP256DH && p.PushAuth == device.PushAuth && p.PushEndpoint == device.PushEndpoint);

            return result;
        }

        public async Task<List<PushDevices>> GetPushDevicesListByUserId(string userId)
        {
            List<PushDevices> deviceList = await webDbContext.PushDevices.Where(m => m.Name == userId).ToListAsync();

            return deviceList;
        }

        public async Task<WebNotification> AddWebNotification(WebNotification notification)
        {
            await webDbContext.WebNotificationsDb.AddAsync(notification);
            await webDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task<WebNotification> UpdateWebNotification(WebNotification notification)
        {
            webDbContext.WebNotificationsDb.Update(notification);
            await webDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task RemoveWebNotification(WebNotification notification)
        {
            webDbContext.WebNotificationsDb.Remove(notification);
            await webDbContext.SaveChangesAsync();
        }

        public async Task<WebNotification> GetWebNotificationById(int id)
        {
            WebNotification notification = await webDbContext.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

            return notification;
        }

        public async Task<List<WebNotification>> GetUsersWebNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await webDbContext.WebNotificationsDb.Where(n => n.To == userId).ToListAsync();

            return usersNotifications;
        }
    }
}
