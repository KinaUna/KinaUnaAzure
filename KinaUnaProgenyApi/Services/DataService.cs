using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class DataService : IDataService
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly WebDbContext _webDbContext;
        public DataService(ProgenyDbContext progenyDbContext, WebDbContext webDbContext)
        {
            _progenyDbContext = progenyDbContext;
            _webDbContext = webDbContext;
        }

        public async Task<MobileNotification> GetMobileNotification(int id)
        {
            MobileNotification notification = await _progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(m => m.NotificationId == id);
            return notification;
        }

        public async Task<MobileNotification> AddMobileNotification(MobileNotification notification)
        {
            _ = _progenyDbContext.MobileNotificationsDb.Add(notification);
            _ = await _progenyDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task<MobileNotification> UpdateMobileNotification(MobileNotification notification)
        {
            MobileNotification updatedNotification = await _progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
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

                _ = _progenyDbContext.MobileNotificationsDb.Update(updatedNotification);
                _ = await _progenyDbContext.SaveChangesAsync();
            }

            return notification;
        }

        public async Task<MobileNotification> DeleteMobileNotification(MobileNotification notification)
        {
            MobileNotification notificationToDelete = await _progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (notificationToDelete != null)
            {
                _ = _progenyDbContext.MobileNotificationsDb.Remove(notificationToDelete);
                _ = await _progenyDbContext.SaveChangesAsync();
            }

            return notification;
        }
        public async Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language)
        {
            List<MobileNotification> notifications = await _progenyDbContext.MobileNotificationsDb.Where(n => n.UserId == userId && n.Language.ToUpper() == language.ToUpper()).ToListAsync();
            if (string.IsNullOrEmpty(language))
            {
                notifications = await _progenyDbContext.MobileNotificationsDb.Where(n => n.UserId == userId).ToListAsync();
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

            _webDbContext.PushDevices.Add(device);
            await _webDbContext.SaveChangesAsync();

            return device;
        }

        public async Task RemovePushDevice(PushDevices device)
        {
            PushDevices existingDevice = await GetPushDevice(device);
            if (existingDevice == null)
            {
                return;
            }

            _webDbContext.PushDevices.Remove(device);
            await _webDbContext.SaveChangesAsync();
        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await _webDbContext.PushDevices.SingleOrDefaultAsync(p => p.Id == id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            var pushDevicesList = await _webDbContext.PushDevices.ToListAsync();

            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices result = await _webDbContext.PushDevices.SingleOrDefaultAsync(p =>
                p.Name == device.Name && p.PushP256DH == device.PushP256DH && p.PushAuth == device.PushAuth && p.PushEndpoint == device.PushEndpoint);

            return result;
        }

        public async Task<List<PushDevices>> GetPushDevicesListByUserId(string userId)
        {
            List<PushDevices> deviceList = await _webDbContext.PushDevices.Where(m => m.Name == userId).ToListAsync();

            return deviceList;
        }

        public async Task<WebNotification> AddWebNotification(WebNotification notification)
        {
            await _webDbContext.WebNotificationsDb.AddAsync(notification);
            await _webDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task<WebNotification> UpdateWebNotification(WebNotification notification)
        {
            _webDbContext.WebNotificationsDb.Update(notification);
            await _webDbContext.SaveChangesAsync();

            return notification;
        }

        public async Task RemoveWebNotification(WebNotification notification)
        {
            _webDbContext.WebNotificationsDb.Remove(notification);
            await _webDbContext.SaveChangesAsync();
        }

        public async Task<WebNotification> GetWebNotificationById(int id)
        {
            WebNotification notification = await _webDbContext.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

            return notification;
        }

        public async Task<List<WebNotification>> GetUsersWebNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await _webDbContext.WebNotificationsDb.Where(n => n.To == userId).ToListAsync();

            return usersNotifications;
        }
    }
}
