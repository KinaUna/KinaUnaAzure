using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class NotificationsListViewModel: BaseViewModel
    {
        public List<WebNotification> NotificationsList { get; set; } = [];
        public WebNotification SelectedNotification { get; set; } = new();
    }
}
