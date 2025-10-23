using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

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
            PermissionLevel[] permissionLevels = (PermissionLevel[])Enum.GetValues(typeof(PermissionLevel));
            foreach (PermissionLevel level in permissionLevels.Where(p => (int)p < (int)PermissionLevel.CreatorOnly))
            {
                // Only show admin level if the current level is admin.
                if (level == PermissionLevel.Admin && PermissionLevel != PermissionLevel.Admin)
                {
                    continue;
                }

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
