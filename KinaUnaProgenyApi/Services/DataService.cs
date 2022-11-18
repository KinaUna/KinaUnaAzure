using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class DataService: IDataService
    {
        private readonly ProgenyDbContext _context;
        
        public DataService(ProgenyDbContext context)
        {
            _context = context;
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
    }
}
