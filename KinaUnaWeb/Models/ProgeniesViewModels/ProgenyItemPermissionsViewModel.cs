using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Models.ProgeniesViewModels
{
    public class ProgenyItemPermissionsViewModel
    {
        public int ProgenyId { get; set; }
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public int ItemId { get; set; }

        public List<ProgenyPermission> ProgenyPermissionsList { get; set; } = [];
        public List<UserInfo> UserList { get; set; } = [];
        public List<UserGroup> UserGroupsList { get; set; } = [];

    }
}
