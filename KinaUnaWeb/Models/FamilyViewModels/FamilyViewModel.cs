using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class FamilyViewModel: BaseItemsViewModel
    {
        public Family Family { get; set; }

        public FamilyViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

    }
}
