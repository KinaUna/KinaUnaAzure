using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class FamilyViewModel: BaseItemsViewModel
    {
        public FamilyDTO FamilyDto { get; init; }

        public FamilyViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

    }
}
