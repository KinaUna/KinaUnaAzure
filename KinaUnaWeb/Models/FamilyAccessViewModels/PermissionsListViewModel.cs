using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class PermissionsListViewModel: BaseItemsViewModel
    {
        public PermissionsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public List<Progeny> Progenies { get; set; } = [];
        public List<Family> Families { get; set; } = [];
        public List<UserGroup> UserGroups { get; set; } = [];
        public List<FamilyPermission> FamilyPermissions { get; set; } = [];
        public List<ProgenyPermission> ProgenyPermissions { get; set; } = [];

    }
}
