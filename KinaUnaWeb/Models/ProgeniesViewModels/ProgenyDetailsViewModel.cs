namespace KinaUnaWeb.Models.ProgeniesViewModels
{
    /// <summary>
    /// ViewModel for the Progeny Details view.
    /// </summary>
    public class ProgenyDetailsViewModel: BaseItemsViewModel
    {
        public ProgenyDetailsViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
        
        public ProgenyInfo ProgenyInfo { get; set; }
    }
}
