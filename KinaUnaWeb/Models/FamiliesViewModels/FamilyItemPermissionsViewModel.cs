using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamilyItemPermissionsViewModel
    {
        public int FamilyId { get; set; }
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public int ItemId { get; set; }
        
        public List<FamilyPermission> FamilyPermissionsList { get; set; } = [];
        public List<UserInfo> UserList { get; set; } = [];
        public List<UserGroup> UserGroupsList { get; set; } = [];

    }
}
