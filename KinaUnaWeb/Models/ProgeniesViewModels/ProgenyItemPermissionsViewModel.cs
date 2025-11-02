using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ProgeniesViewModels
{
    public class ProgenyItemPermissionsViewModel
    {
        public int LanguageId { get; set; }
        public int ProgenyId { get; set; }
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public int ItemId { get; set; }
        public List<TimelineItemPermission> ItemPermissionsList { get; set; } = [];
        public List<ProgenyPermission> ProgenyPermissionsList { get; set; } = [];
        public List<UserInfo> UserList { get; set; } = [];
        public List<UserGroup> UserGroupsList { get; set; } = [];
        public List<SelectListItem> PermissionLevelsSelectListItems { get; set; } = [];
        public bool IsUserAccessManager { get; set; }
        public List<SelectListItem> PermissionTypeSelectListItems { get; set; } = [];

        public void SetInitialPermissionLevelsSelectListItems()
        {
            foreach (PermissionLevel permissionLevel in System.Enum.GetValues<PermissionLevel>())
            {
                if (permissionLevel != PermissionLevel.Add && permissionLevel != PermissionLevel.CreatorOnly && permissionLevel != PermissionLevel.Private)
                {
                    PermissionLevelsSelectListItems.Add(new SelectListItem
                    {
                        Value = ((int)permissionLevel).ToString(),
                        Text = permissionLevel.ToString()
                    });
                }
            }
        }

        public void SetPermissionTypeSelectListItems(int selectedType)
        {
            IsUserAccessManager = ProgenyPermissionsList.Count > 0; // If the user has access to the list, they are an access manager.
            PermissionTypeSelectListItems = [new SelectListItem { Value = "0", Text = "Inherit" }];

            // Only add "Only me" if the item is new (ItemId == 0) or if it is already set to "Only me".
            // If a user wants to change an existing item to Only Me they will have to copy it, set the copy to Only Me and delete the original.
            if (ItemId == 0 || selectedType == 1)
            {
                PermissionTypeSelectListItems.Add(new() { Value = "1", Text = "Only me" });
            }
            // Only add "Private" if the item is new (ItemId == 0) or if it is already set to "Private".
            // If a user wants to change an existing item to Private they will have to copy it, set the copy to Private and delete the original.
            if (ItemId == 0 || selectedType == 2)
            {
                PermissionTypeSelectListItems.Add(new() { Value = "2", Text = "Private" });
            }

            if (IsUserAccessManager)
            {
                PermissionTypeSelectListItems.Add(new SelectListItem { Value = "3", Text = "Custom" });
            }

            foreach (SelectListItem item in PermissionTypeSelectListItems)
            {
                if (item.Value == selectedType.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public List<SelectListItem> CreatePermissionLevelsSelectListItems(int selectedLevel, bool isAdmin)
        {
            // We do not want to have Add, CreatorOnly or Private as options in the select list.
            if ((PermissionLevel)selectedLevel == PermissionLevel.Add || (PermissionLevel)selectedLevel >= PermissionLevel.CreatorOnly)
            {
                selectedLevel = (int)PermissionLevel.View;
            }

            List<SelectListItem> permissionLevels = [];
            foreach (SelectListItem item in PermissionLevelsSelectListItems)
            {
                if (item.Value == selectedLevel.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }

                // Only show Admin level if the user is an admin.
                if (selectedLevel != (int)PermissionLevel.Admin && item.Value == ((int)PermissionLevel.Admin).ToString())
                {
                    if (!isAdmin)
                    {
                        continue;
                    }
                }

                permissionLevels.Add(item);
            }
            
            return permissionLevels;
        }
    }
}
