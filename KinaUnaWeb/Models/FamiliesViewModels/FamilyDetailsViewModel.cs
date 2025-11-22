using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamilyDetailsViewModel : BaseItemsViewModel
    {
        public FamilyDetailsViewModel()
        {
            
        }
        public FamilyDetailsViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
        public Family Family { get; set; } = new();
    }
}
