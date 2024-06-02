using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.TypeScriptModels.Notifications
{
    public class WebNotificationsList
    {
        public List<WebNotification> NotificationsList { get; set; } = [];
        public int AllNotificationsCount { get; set; } = 0;
        public int RemainingItemsCount { get; set; } = 0;
    }
}
