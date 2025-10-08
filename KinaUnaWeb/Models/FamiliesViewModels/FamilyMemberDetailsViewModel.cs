using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamilyMemberDetailsViewModel: BaseItemsViewModel
    {
        public FamilyMemberDetailsViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public FamilyMember FamilyMember { get; set; } = new();
        public Family Family { get; set; }
        public bool IsCurrentUserFamilyAdmin { get; set; }
    }
}
