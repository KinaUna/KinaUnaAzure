using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class PermissionViewModel: BaseItemsViewModel
    {
        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public PermissionViewModel()
        {

        }

        public PermissionViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetPermissionsLevelsList();
        }

        public string Email { get; set; }
        public ProgenyPermission ProgenyPermission { get; set; } = new();
        public FamilyPermission FamilyPermission { get; set; } = new();
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;
        public List<SelectListItem> PermissionLevelsList { get; set; } = [];

        public void SetPermissionsLevelsList()
        {
            PermissionLevelsList = [];
            PermissionLevel[] permissionLevels = (PermissionLevel[])Enum.GetValues(typeof(PermissionLevel));
            foreach (PermissionLevel level in permissionLevels.Where(p => (int)p < (int)PermissionLevel.Admin))
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
