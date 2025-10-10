using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class PermissionViewModel: BaseItemsViewModel
    {
        public PermissionViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public ProgenyPermission ProgenyPermission { get; set; } = new();
        public FamilyPermission FamilyPermission { get; set; } = new();

    }
}
