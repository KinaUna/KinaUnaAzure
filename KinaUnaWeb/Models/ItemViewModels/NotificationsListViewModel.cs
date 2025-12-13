using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotificationsListViewModel: BaseItemsViewModel
    {
        public List<WebNotification> NotificationsList { get; set; }
        public WebNotification SelectedNotification { get; set; }

        public int Start { get; set; } = 0;
        public int Count { get; set; } = 10;

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
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
