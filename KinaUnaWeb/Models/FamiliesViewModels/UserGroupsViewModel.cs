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

        public List<Progeny> ProgenyList { get; set; } = new List<Progeny>();
        public List<Family> FamiliesList { get; set; } = new List<Family>();
        public List<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    }
}
