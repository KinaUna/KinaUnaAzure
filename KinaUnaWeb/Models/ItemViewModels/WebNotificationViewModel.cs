namespace KinaUnaWeb.Models.ItemViewModels
{
    public class WebNotificationViewModel: BaseItemsViewModel
    {
        public int Id { get; set; } = 0;
        

        public WebNotification WebNotification { get; set; } = new();

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public WebNotificationViewModel()
        {
            
        }

        public WebNotificationViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
