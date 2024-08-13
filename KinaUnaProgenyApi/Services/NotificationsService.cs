using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class NotificationsService(ProgenyDbContext progenyDbContext, IDistributedCache cache) : INotificationsService
    {
        private static DistributedCacheEntryOptions GetCacheEntryOptions()
        {
            DistributedCacheEntryOptions cacheOptions = new();
            cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            return cacheOptions;
        }

        public async Task<MobileNotification> GetMobileNotification(int id)
        {
            MobileNotification notification = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(m => m.NotificationId == id);
            return notification;
        }

        public async Task<MobileNotification> AddMobileNotification(MobileNotification notification)
        {
            _ = await progenyDbContext.MobileNotificationsDb.AddAsync(notification);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "String comparison does not work with database queries.")]
        public async Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language)
        {
            List<MobileNotification> notifications = await progenyDbContext.MobileNotificationsDb.AsNoTracking().Where(n => n.UserId == userId && n.Language.ToUpper() == language.ToUpper()).ToListAsync();
            if (string.IsNullOrEmpty(language))
            {
                notifications = await progenyDbContext.MobileNotificationsDb.AsNoTracking().Where(n => n.UserId == userId).ToListAsync();
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

            await progenyDbContext.PushDevices.AddAsync(device);
            await progenyDbContext.SaveChangesAsync();

            return device;
        }

        public async Task RemovePushDevice(PushDevices device)
        {
            PushDevices existingDevice = await GetPushDevice(device);
            if (existingDevice == null)
            {
                return;
            }

            progenyDbContext.PushDevices.Remove(device);
            await progenyDbContext.SaveChangesAsync();
        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await progenyDbContext.PushDevices.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            List<PushDevices> pushDevicesList = await progenyDbContext.PushDevices.AsNoTracking().ToListAsync();

            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices result = await progenyDbContext.PushDevices.AsNoTracking().SingleOrDefaultAsync(p =>
                p.Name == device.Name && p.PushP256DH == device.PushP256DH && p.PushAuth == device.PushAuth && p.PushEndpoint == device.PushEndpoint);

            return result;
        }

        public async Task<List<PushDevices>> GetPushDevicesListByUserId(string userId)
        {
            List<PushDevices> deviceList = await progenyDbContext.PushDevices.AsNoTracking().Where(m => m.Name == userId).ToListAsync();

            return deviceList;
        }

        private async Task<WebNotification> SetWebNotificationInCache(int id)
        {
            WebNotification notification = await progenyDbContext.WebNotificationsDb.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id);
            if (notification == null) return null;
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotification" + notification.Id, JsonConvert.SerializeObject(notification), GetCacheEntryOptions());

            List<WebNotification> notificationsList = await progenyDbContext.WebNotificationsDb.AsNoTracking().Where(n => n.To == notification.To).ToListAsync();
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + notification.To, JsonConvert.SerializeObject(notificationsList), GetCacheEntryOptions());

            return notification;
        }

        private async Task<WebNotification> GetWebNotificationFromCache(int id)
        {
            WebNotification notification = new();
            string cachedNotification = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotification" + id);
            if (!string.IsNullOrEmpty(cachedNotification))
            {
                notification = JsonConvert.DeserializeObject<WebNotification>(cachedNotification);
            }

            return notification;
        }

        private async Task RemoveWebNotificationFromCache(int id, string userId)
        {
            await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "webnotification" + id);

            List<WebNotification> notificationsList = await progenyDbContext.WebNotificationsDb.AsNoTracking().Where(n => n.To == userId).ToListAsync();
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + userId, JsonConvert.SerializeObject(notificationsList), GetCacheEntryOptions());
        }

        public async Task<WebNotification> AddWebNotification(WebNotification notification)
        {
            await progenyDbContext.WebNotificationsDb.AddAsync(notification);
            await progenyDbContext.SaveChangesAsync();

            _ = await SetWebNotificationInCache(notification.Id);

            return notification;
        }

        public async Task<WebNotification> UpdateWebNotification(WebNotification notification)
        {
            WebNotification notificationToUpdate = await progenyDbContext.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == notification.Id);
            if (notificationToUpdate == null) return null;
            
            notificationToUpdate.To = notification.To;
            notificationToUpdate.From = notification.From;
            notificationToUpdate.Message = notification.Message;
            notificationToUpdate.DateTime = notification.DateTime;
            notificationToUpdate.IsRead = notification.IsRead;
            notificationToUpdate.Link = notification.Link;
            notificationToUpdate.Title = notification.Title;
            notificationToUpdate.Type = notification.Type;

            progenyDbContext.WebNotificationsDb.Update(notificationToUpdate);
            await progenyDbContext.SaveChangesAsync();

            _ = await SetWebNotificationInCache(notification.Id);

            return notification;
        }

        public async Task RemoveWebNotification(WebNotification notification)
        {
            WebNotification notificationToDelete = await progenyDbContext.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == notification.Id);
            if (notificationToDelete == null) return;
            
            progenyDbContext.WebNotificationsDb.Remove(notificationToDelete);
            await progenyDbContext.SaveChangesAsync();

            await RemoveWebNotificationFromCache(notification.Id, notification.To);
        }

        public async Task<WebNotification> GetWebNotificationById(int id)
        {
            WebNotification notification = await GetWebNotificationFromCache(id);
            if (notification == null || notification.Id == 0)
            {
                notification = await SetWebNotificationInCache(id);
            }

            return notification;
        }

        private async Task<List<WebNotification>> GetUsersWebNotificationsFromCache(string userId)
        {
            List<WebNotification> notificationsList = [];
            string cachedNotifications = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + userId);
            if (!string.IsNullOrEmpty(cachedNotifications))
            {
                notificationsList = JsonConvert.DeserializeObject<List<WebNotification>>(cachedNotifications);
            }

            return notificationsList;
        }

        private async Task<List<WebNotification>> SetUsersWebNotificationsInCache(string userId)
        {
            List<WebNotification> notificationsList = await progenyDbContext.WebNotificationsDb.AsNoTracking().Where(n => n.To == userId).ToListAsync();
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + userId, JsonConvert.SerializeObject(notificationsList), GetCacheEntryOptions());

            return notificationsList;
        }

        public async Task<List<WebNotification>> GetUsersWebNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await GetUsersWebNotificationsFromCache(userId);
            if (usersNotifications == null || usersNotifications.Count == 0)
            {
                usersNotifications = await SetUsersWebNotificationsInCache(userId);
            }

            return usersNotifications;
        }

        public async Task<List<WebNotification>> GetLatestWebNotifications(string userId, int start, int count, bool unreadOnly)
        {
            List<WebNotification> notificationsList = await GetUsersWebNotifications(userId);
            notificationsList = [.. notificationsList.OrderByDescending(n => n.DateTime)];

            if (unreadOnly)
            {
                notificationsList = notificationsList.Where(n => !n.IsRead).ToList();
            }

            notificationsList = notificationsList.Skip(start).Take(count).ToList();

            return notificationsList;

        }

        public async Task<int> GetUsersNotificationsCount(string userId)
        {
            List<WebNotification> notificationsList = await GetUsersWebNotifications(userId);
            return notificationsList.Count;
        }
    }
}
