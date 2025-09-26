using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamilyElementViewModel: BaseItemsViewModel
    {
        public FamilyElementViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
        public Family Family { get; set; } = new Family();
    }
}
