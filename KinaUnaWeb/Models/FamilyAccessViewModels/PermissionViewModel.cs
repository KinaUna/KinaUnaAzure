using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.FamilyAccessViewModels
{
    public class PermissionViewModel: BaseItemsViewModel
    {
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
