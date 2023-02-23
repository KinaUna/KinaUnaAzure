using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class WebNotificationViewModel: BaseItemsViewModel
    {
        public WebNotification WebNotification { get; set; } = new WebNotification();

        public WebNotificationViewModel()
        {
            
        }

        public WebNotificationViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
