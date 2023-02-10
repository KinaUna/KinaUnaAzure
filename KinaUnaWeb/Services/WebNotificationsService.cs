using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaWeb.Services
{
    public class WebNotificationsService: IWebNotificationsService
    {
        private readonly WebDbContext _context;

        public WebNotificationsService(WebDbContext context)
        {
            _context = context;
        }

        public async Task<WebNotification> SaveNotification(WebNotification notification)
        {
            await _context.WebNotificationsDb.AddAsync(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<WebNotification> UpdateNotification(WebNotification notification)
        {
            _context.WebNotificationsDb.Update(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task RemoveNotification(WebNotification notification)
        {
            _context.WebNotificationsDb.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<WebNotification> GetNotificationById(int id)
        {
            WebNotification notification = await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

            return notification;
        }

        public async Task<List<WebNotification>> GetUsersNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await _context.WebNotificationsDb.Where(n => n.To == userId).ToListAsync();

            return usersNotifications;
        }
    }
}
