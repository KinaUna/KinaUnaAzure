using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.FamilyViewModels
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
        public UserAccess UserAccess { get; set; }
    }
}
