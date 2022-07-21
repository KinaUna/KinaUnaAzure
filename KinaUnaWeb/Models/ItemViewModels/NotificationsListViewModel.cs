using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotificationsListViewModel: BaseViewModel
    {
        public List<WebNotification> NotificationsList { get; set; }
        public WebNotification SelectedNotification { get; set; }

        public NotificationsListViewModel()
        {
            NotificationsList = new List<WebNotification>();
        }
    }
}
