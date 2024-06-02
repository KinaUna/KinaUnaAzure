using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotificationsListViewModel: BaseItemsViewModel
    {
        public List<WebNotification> NotificationsList { get; set; }
        public WebNotification SelectedNotification { get; set; }

        public int Start { get; set; } = 0;
        public int Count { get; set; } = 10;

        public NotificationsListViewModel()
        {
            NotificationsList = [];
        }

        public NotificationsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
