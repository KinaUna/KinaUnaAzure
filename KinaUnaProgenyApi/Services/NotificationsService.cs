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
            _ = cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            return cacheOptions;
        }

        /// <summary>
        /// Gets a MobileNotification by NotificationId.
        /// </summary>
        /// <param name="id">The NotificationId of the Notification to get.</param>
        /// <returns>The Notification with the given NotificationId. Null if it doesn't exist.</returns>
        public async Task<MobileNotification> GetMobileNotification(int id)
        {
            MobileNotification notification = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(m => m.NotificationId == id);
            return notification;
        }

        /// <summary>
        /// Adds a new MobileNotification to the database.
        /// </summary>
        /// <param name="notification">The MobileNotification to add.</param>
        /// <returns>The added MobileNotification.</returns>
        public async Task<MobileNotification> AddMobileNotification(MobileNotification notification)
        {
            _ = progenyDbContext.MobileNotificationsDb.Add(notification);
            _ = await progenyDbContext.SaveChangesAsync();

            return notification;
        }

        /// <summary>
        /// Updates a MobileNotification in the database.
        /// </summary>
        /// <param name="notification">The MobileNotification with the updated properties.</param>
        /// <returns>The updated MobileNotification.</returns>
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

        /// <summary>
        /// Deletes a MobileNotification from the database.
        /// </summary>
        /// <param name="notification">The MobileNotification to delete.</param>
        /// <returns>The deleted Notification.</returns>
        public async Task<MobileNotification> DeleteMobileNotification(MobileNotification notification)
        {
            MobileNotification notificationToDelete = await progenyDbContext.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (notificationToDelete == null) return null;

            _ = progenyDbContext.MobileNotificationsDb.Remove(notificationToDelete);
            _ = await progenyDbContext.SaveChangesAsync();

            return notification;
        }

        /// <summary>
        /// Gets a list of all MobileNotifications for a user in a specific language.
        /// </summary>
        /// <param name="userId">The UserId of the user to get Notifications for.</param>
        /// <param name="language">The Language of the Notifications.</param>
        /// <returns>List of Notifications.</returns>
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

        /// <summary>
        /// Adds a new PushDevice to the database.
        /// </summary>
        /// <param name="device">The PushDevice to add.</param>
        /// <returns>The added PushDevice.</returns>
        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            PushDevices existingDevice = await GetPushDevice(device);
            if (existingDevice != null)
            {
                return null;
            }

            _ = progenyDbContext.PushDevices.Add(device);
            _ = await progenyDbContext.SaveChangesAsync();

            return device;
        }

        /// <summary>
        /// Deletes a PushDevice from the database.
        /// </summary>
        /// <param name="device">The PushDevice to remove.</param>
        /// <returns></returns>
        public async Task RemovePushDevice(PushDevices device)
        {
            PushDevices existingDevice = await GetPushDevice(device);
            if (existingDevice == null)
            {
                return;
            }

            _ = progenyDbContext.PushDevices.Remove(device);
            _ = await progenyDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a PushDevice by Id.
        /// </summary>
        /// <param name="id">The Id of the PushDevice to get.</param>
        /// <returns>The PushDevice with the given Id. Null if it doesn't exist.</returns>
        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await progenyDbContext.PushDevices.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);

            return device;
        }

        /// <summary>
        /// Gets a list of all PushDevices in the database.
        /// </summary>
        /// <returns>List of all PushDevices.</returns>
        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            List<PushDevices> pushDevicesList = await progenyDbContext.PushDevices.AsNoTracking().ToListAsync();

            return pushDevicesList;
        }

        /// <summary>
        /// Gets a PushDevice by the PushDevice's Name, PushP256DH, PushAuth, and PushEndPoint properties.
        /// </summary>
        /// <param name="device">The PushDevice to get.</param>
        /// <returns>The PushDevice.</returns>
        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices result = await progenyDbContext.PushDevices.AsNoTracking().SingleOrDefaultAsync(p =>
                p.Name == device.Name && p.PushP256DH == device.PushP256DH && p.PushAuth == device.PushAuth && p.PushEndpoint == device.PushEndpoint);

            return result;
        }

        /// <summary>
        /// Gets a list of all PushDevices for a user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>The PushDevice if found. Null if it doesn't exist.</returns>
        public async Task<List<PushDevices>> GetPushDevicesListByUserId(string userId)
        {
            List<PushDevices> deviceList = await progenyDbContext.PushDevices.AsNoTracking().Where(m => m.Name == userId).ToListAsync();

            return deviceList;
        }

        /// <summary>
        /// Gets a WebNotification by Id from the database and sets it in the cache.
        /// Also updates the list of WebNotifications for the user in the cache.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get and set in cache.</param>
        /// <returns>The WebNotification with the Given Id. Null if not found.</returns>
        private async Task<WebNotification> SetWebNotificationInCache(int id)
        {
            WebNotification notification = await progenyDbContext.WebNotificationsDb.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id);
            if (notification == null) return null;
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotification" + notification.Id, JsonConvert.SerializeObject(notification), GetCacheEntryOptions());

            List<WebNotification> notificationsList = await progenyDbContext.WebNotificationsDb.AsNoTracking().Where(n => n.To == notification.To).ToListAsync();
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + notification.To, JsonConvert.SerializeObject(notificationsList), GetCacheEntryOptions());

            return notification;
        }

        /// <summary>
        /// Gets a WebNotification by Id from the cache.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get.</param>
        /// <returns>The WebNotification with the given Id. If not found a new WebNotification, check for WebNotification.Id != 0 to verify an entity exists.</returns>
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

        /// <summary>
        /// Removes a WebNotification from the cache and updates the list of WebNotifications for the user in the cache.
        /// </summary>
        /// <param name="id">The Id of the WebNotification.</param>
        /// <param name="userId">The user's UserId.</param>
        /// <returns></returns>
        private async Task RemoveWebNotificationFromCache(int id, string userId)
        {
            await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "webnotification" + id);

            List<WebNotification> notificationsList = await progenyDbContext.WebNotificationsDb.AsNoTracking().Where(n => n.To == userId).ToListAsync();
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + userId, JsonConvert.SerializeObject(notificationsList), GetCacheEntryOptions());
        }

        /// <summary>
        /// Adds a new WebNotification to the database and sets it in the cache.
        /// </summary>
        /// <param name="notification">The WebNotification to add.</param>
        /// <returns>The added WebNotification.</returns>
        public async Task<WebNotification> AddWebNotification(WebNotification notification)
        {
            _ = progenyDbContext.WebNotificationsDb.Add(notification);
            _ = await progenyDbContext.SaveChangesAsync();

            _ = await SetWebNotificationInCache(notification.Id);

            return notification;
        }

        /// <summary>
        /// Updates a WebNotification in the database and sets it in the cache.
        /// </summary>
        /// <param name="notification">The WebNotification with updated properties.</param>
        /// <returns>The updated WebNotification.</returns>
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

            _ = progenyDbContext.WebNotificationsDb.Update(notificationToUpdate);
            _ = await progenyDbContext.SaveChangesAsync();

            _ = await SetWebNotificationInCache(notification.Id);

            return notification;
        }

        /// <summary>
        /// Removes a WebNotification from the database and the cache.
        /// </summary>
        /// <param name="notification">The WebNotification to remove.</param>
        /// <returns></returns>
        public async Task RemoveWebNotification(WebNotification notification)
        {
            WebNotification notificationToDelete = await progenyDbContext.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == notification.Id);
            if (notificationToDelete == null) return;

            _ = progenyDbContext.WebNotificationsDb.Remove(notificationToDelete);
            _ = await progenyDbContext.SaveChangesAsync();

            await RemoveWebNotificationFromCache(notification.Id, notification.To);
        }

        /// <summary>
        /// Gets a WebNotification by Id.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get.</param>
        /// <returns>The WebNotification with the given Id. Null if the WebNotification doesn't exist.</returns>
        public async Task<WebNotification> GetWebNotificationById(int id)
        {
            WebNotification notification = await GetWebNotificationFromCache(id);
            if (notification == null || notification.Id == 0)
            {
                notification = await SetWebNotificationInCache(id);
            }

            return notification;
        }

        /// <summary>
        /// Gets a list of all WebNotifications for a user from the cache.
        /// </summary>
        /// <param name="userId">The UserId of the user to get all WebNotifications for.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets all WebNotifications for a user from the database and sets them in the cache.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>List of all WebNotifications for the user.</returns>
        private async Task<List<WebNotification>> SetUsersWebNotificationsInCache(string userId)
        {
            List<WebNotification> notificationsList = await progenyDbContext.WebNotificationsDb.AsNoTracking().Where(n => n.To == userId).ToListAsync();
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "webnotifications" + userId, JsonConvert.SerializeObject(notificationsList), GetCacheEntryOptions());

            return notificationsList;
        }

        /// <summary>
        /// Gets all WebNotifications for a user.
        /// First tries to get the notifications from the cache, if none are found then gets them from the database and sets the cache.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>List of WebNotifications.</returns>
        public async Task<List<WebNotification>> GetUsersWebNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await GetUsersWebNotificationsFromCache(userId);
            if (usersNotifications == null || usersNotifications.Count == 0)
            {
                usersNotifications = await SetUsersWebNotificationsInCache(userId);
            }

            return usersNotifications;
        }

        /// <summary>
        /// Gets a list of the latest WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user to get Notifications for.</param>
        /// <param name="start">Number of WebNotifications to skip.</param>
        /// <param name="count">Number of WebNotifications to get.</param>
        /// <param name="unreadOnly">Filter the list, if unreadOnly is true only include the WebNotification with IsRead set to false.</param>
        /// <returns>List of WebNotifications.</returns>
        public async Task<List<WebNotification>> GetLatestWebNotifications(string userId, int start, int count, bool unreadOnly)
        {
            List<WebNotification> notificationsList = await GetUsersWebNotifications(userId);
            notificationsList = [.. notificationsList.OrderByDescending(n => n.DateTime)];

            if (unreadOnly)
            {
                notificationsList = [.. notificationsList.Where(n => !n.IsRead)];
            }

            notificationsList = [.. notificationsList.Skip(start).Take(count)];

            return notificationsList;

        }

        /// <summary>
        /// Gets the number of WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user.</param>
        /// <returns>Number of WebNotifications found for the user.</returns>
        public async Task<int> GetUsersNotificationsCount(string userId)
        {
            List<WebNotification> notificationsList = await GetUsersWebNotifications(userId);
            return notificationsList.Count;
        }
    }
}
