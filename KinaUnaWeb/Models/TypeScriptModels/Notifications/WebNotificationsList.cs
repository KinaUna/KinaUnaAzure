using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Notifications
{
    public class WebNotificationsList
    {
        public List<WebNotification> NotificationsList { get; set; } = [];
        public int AllNotificationsCount { get; set; } = 0;
        public int RemainingItemsCount { get; set; } = 0;
    }
}
