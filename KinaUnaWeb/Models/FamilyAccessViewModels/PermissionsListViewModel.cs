using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class PermissionsListViewModel: BaseItemsViewModel
    {
        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public PermissionsListViewModel()
        {

        }

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
