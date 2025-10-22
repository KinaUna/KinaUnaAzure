using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class UserGroupViewModel: BaseItemsViewModel
    {
        public UserGroupViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetPermissionsLevelsList();
            UserGroup = new UserGroup();
        }

        public UserGroup UserGroup { get; set; }
        public UserGroupMember UserGroupMember {get; set;}

        public List<FamilyPermission> FamilyPermissions { get; set; } = [];
        public List<ProgenyPermission> ProgenyPermissions { get; set; } = [];

        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;
        public List<SelectListItem> PermissionLevelsList { get; set; } = [];

        public void SetPermissionsLevelsList()
        {
            PermissionLevelsList = [];
            foreach (PermissionLevel level in (PermissionLevel[])Enum.GetValues(typeof(PermissionLevel)))
            {
                PermissionLevelsList.Add(new SelectListItem
                {
                    Text = level.ToString(),
                    Value = ((int)level).ToString(),
                    Selected = level == PermissionLevel
                });
            }
        }
    }
}
