using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models.DTOs
{
    public class ItemPermissionsDto
    {
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public int ItemId { get; set; }
        public bool InheritPermission { get; set; }
        public List<TimelineItemPermission> ItemPermissionsList { get; set; } = new List<TimelineItemPermission>();
    }
}
