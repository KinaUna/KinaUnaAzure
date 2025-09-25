using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    public class PermissionAuditLog
    {
        [Key]
        public int PermissionAuditLogId { get; set; }
        public int EntityId { get; set; } // FamilyId, ProgenyId or TimelineItemId
        public string EntityType { get; set; } = string.Empty; // "Family", "Progeny" or "TimelineItem"
        public PermissionAction Action { get; set; } = PermissionAction.Unknown; // "Add", "Update", "Delete"
        public string ChangedBy { get; set; } = string.Empty; // UserId or email of the user who made the change
        public DateTime ChangeTime { get; set; }
        public string ItemBefore { get; set; } = string.Empty; // JSON or text details of the change
        public string ItemAfter { get; set; } = string.Empty; // JSON or text details of the change
    }
}
