using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotificationsListViewModel: BaseItemsViewModel
    {
        public List<WebNotification> NotificationsList { get; set; }
        public WebNotification SelectedNotification { get; set; }

        public NotificationsListViewModel()
        {
            NotificationsList = new List<WebNotification>();
        }

        public NotificationsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
