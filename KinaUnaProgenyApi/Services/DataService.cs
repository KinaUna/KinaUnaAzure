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
            MobileNotification notificationToUpdate = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (notificationToUpdate == null) return null;

            notificationToUpdate.IconLink = notification.IconLink;
            notificationToUpdate.Title = notification.Title;
            notificationToUpdate.ItemId = notification.ItemId;
            notificationToUpdate.ItemType = notification.ItemType;
            notificationToUpdate.Language = notification.Language;
            notificationToUpdate.Message = notification.Message;
            notificationToUpdate.Read = notification.Read;
            notificationToUpdate.Time = notification.Time;
            notificationToUpdate.UserId = notification.UserId;

            _ = progenyDbContext.MobileNotificationsDb.Update(notificationToUpdate);
            _ = await progenyDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task<MobileNotification> DeleteMobileNotification(MobileNotification notification)
        {
            MobileNotification notificationToDelete = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (notificationToDelete == null) return null;

            _ = progenyDbContext.MobileNotificationsDb.Remove(notificationToDelete);
            _ = await progenyDbContext.SaveChangesAsync();

            return notification;
        }
        public async Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language)
        {
            List<MobileNotification> notifications = await progenyDbContext.MobileNotificationsDb.Where(n => n.UserId == userId && n.Language.Equals(language, System.StringComparison.CurrentCultureIgnoreCase)).ToListAsync();
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
                return null;
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
            List<PushDevices> pushDevicesList = await webDbContext.PushDevices.ToListAsync();

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
