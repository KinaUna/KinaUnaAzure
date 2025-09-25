using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    public class UserGroupAuditLog
    {
        [Key]
        public int UserGroupAuditLogId { get; set; }

        public int UserGroupId { get; set; } = 0;
        public int UserGroupMemberId { get; set; } = 0;

        public UserGroupAction Action { get; set; } = UserGroupAction.Unknown; // E.g., CreateGroup, UpdateGroup, DeleteGroup, AddGroupMember, UpdateGroupMember, RemoveGroupMember
        [MaxLength(256)]
        public string EntityType { get; set; } = string.Empty; // E.g., "UserGroup", "UserGroupMember", etc.
        [MaxLength(8192)]
        public string EntityBefore { get; set; } = string.Empty; // JSON or text details of the item before the change

        [MaxLength(8192)]
        public string EntityAfter { get; set; } = string.Empty; // JSON or text details of the item after the change

        [MaxLength(256)]
        public string ChangedBy { get; set; }

        public DateTime ChangeTime { get; set; }
    }
}
