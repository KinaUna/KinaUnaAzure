using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamilyItemPermissionsViewModel
    {
        public int LanguageId { get; set; }
        public int FamilyId { get; set; }
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public int ItemId { get; set; }
        
        public List<TimelineItemPermission> ItemPermissionsList { get; set; } = [];
        public List<FamilyPermission> FamilyPermissionsList { get; set; } = [];
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
            IsUserAccessManager = FamilyPermissionsList.Count > 0; // If the user has access to the list, they are an access manager.
            PermissionTypeSelectListItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "Inherit" },
                new SelectListItem { Value = "1", Text = "Only me" },
                new SelectListItem { Value = "2", Text = "Private" },
            };

            if (IsUserAccessManager)
            {
                PermissionTypeSelectListItems.Add(new SelectListItem { Value = "3", Text = "Custom" });
            }

            foreach (SelectListItem item in PermissionTypeSelectListItems)
            {
                if (item.Value == selectedType.ToString())
                {
                    item.Selected = true;
                    break;
                }
            }
        }

        public List<SelectListItem> CreatePermissionLevelsSelectListItems(int selectedLevel)
        {
            List<SelectListItem> permissionLevels = [];
            foreach (SelectListItem item in PermissionLevelsSelectListItems)
            {
                if (item.Value == selectedLevel.ToString())
                {
                    item.Selected = true;
                    break;
                }

                permissionLevels.Add(item);
            }

            return permissionLevels;
        }
    }
}
