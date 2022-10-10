using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class NotificationsListViewModel: BaseViewModel
    {
        public List<WebNotification> NotificationsList { get; set; } = new List<WebNotification>();
        public WebNotification SelectedNotification { get; set; } = new WebNotification();
    }
}
