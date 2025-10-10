using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class UserGroupViewModel: BaseItemsViewModel
    {
        public UserGroupViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public UserGroup UserGroup { get; set; }
        
        public List<FamilyPermission> FamilyPermissions { get; set; } = [];
        public List<ProgenyPermission> ProgenyPermissions { get; set; } = [];
    }
}
