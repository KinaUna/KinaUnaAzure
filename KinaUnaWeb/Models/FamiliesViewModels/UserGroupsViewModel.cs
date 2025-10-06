using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class UserGroupsViewModel: BaseItemsViewModel
    {
        public UserGroupsViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public List<Progeny> Progenies { get; set; }
        public List<Family> Families { get; set; }
        public List<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    }
}
