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
        public async Task AddMobileNotification(MobileNotification notification)
        {
            _ = _context.MobileNotificationsDb.Add(notification);
            _ = await _context.SaveChangesAsync();
        }

        public async Task<MobileNotification> UpdateMobileNotification(MobileNotification notification)
        {
            _ = _context.MobileNotificationsDb.Update(notification);
            _ = await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<MobileNotification> DeleteMobileNotification(MobileNotification notification)
        {
            _ = _context.MobileNotificationsDb.Remove(notification);
            _ = await _context.SaveChangesAsync();

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
