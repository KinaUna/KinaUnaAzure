using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace KinaUnaProgenyApi.Services
{
    public class DataService: IDataService
    {
        private readonly ProgenyDbContext _context;
        private readonly WebDbContext _webDbContext;
        public DataService(ProgenyDbContext context, WebDbContext webDbContext)
        {
            _context = context;
            _webDbContext = webDbContext;
        }

        public async Task<MobileNotification> GetMobileNotification(int id)
        {
            MobileNotification notification = await _context.MobileNotificationsDb.SingleOrDefaultAsync(m => m.NotificationId == id);
            return notification;
        }

        public async Task<MobileNotification> AddMobileNotification(MobileNotification notification)
        {
            _ = _context.MobileNotificationsDb.Add(notification);
            _ = await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<MobileNotification> UpdateMobileNotification(MobileNotification notification)
        {
            MobileNotification updatedNotification = await _context.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
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
                
                _ = _context.MobileNotificationsDb.Update(updatedNotification);
                _ = await _context.SaveChangesAsync();
            }
            
            return notification;
        }

        public async Task<MobileNotification> DeleteMobileNotification(MobileNotification notification)
        {
            MobileNotification notificationToDelete = await _context.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == notification.NotificationId);
            if (notificationToDelete != null)
            {
                _ = _context.MobileNotificationsDb.Remove(notificationToDelete);
                _ = await _context.SaveChangesAsync();
            }
            
            return notification;
        }
        public async Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language)
        {
            List<MobileNotification> notifications = await _context.MobileNotificationsDb.Where(n => n.UserId == userId && n.Language.ToUpper() == language.ToUpper()).ToListAsync();
            if (string.IsNullOrEmpty(language))
            {
                notifications = await _context.MobileNotificationsDb.Where(n => n.UserId == userId).ToListAsync();
            }

            return notifications;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            _webDbContext.PushDevices.Add(device);
            await _context.SaveChangesAsync();

            return device;
        }

        public async Task RemovePushDevice(PushDevices device)
        {
            _webDbContext.PushDevices.Remove(device);
            await _context.SaveChangesAsync();
        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await _webDbContext.PushDevices.SingleOrDefaultAsync(m => m.Id == id);

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

        public async Task<List<PushDevices>> GetPushDeviceByUserId(string userId)
        {
            List<PushDevices> deviceList = await _webDbContext.PushDevices.Where(m => m.Name == userId).ToListAsync();

            return deviceList;
        }
    }
}
