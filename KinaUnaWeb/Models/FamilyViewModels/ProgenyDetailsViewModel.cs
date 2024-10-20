using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class ProgenyDetailsViewModel: BaseItemsViewModel
    {
        public ProgenyDetailsViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
        
        public ProgenyInfo ProgenyInfo { get; set; }
    }
}
