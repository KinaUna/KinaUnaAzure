using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Represents an audit log entry for changes made to permissions within the system.
    /// </summary>
    /// <remarks>This class is used to track changes to permissions, including the type of permission, the
    /// action performed, the user who made the change, and the state of the item before and after the change. It
    /// supports logging for different types of permissions, such as family, progeny, and timeline item
    /// permissions.</remarks>
    public class PermissionAuditLog
    {
        /// <summary>
        /// Gets or sets the unique identifier for the permission audit log entry.
        /// </summary>
        [Key]
        public int PermissionAuditLogId { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the progeny associated with this entity.
        /// </summary>
        public int ProgenyId { get; set; } = 0; // For ProgenyPermission and TimelineItemPermission only

        /// <summary>
        /// Gets or sets the identifier for the family associated with this entity.
        /// </summary>
        public int FamilyId { get; set; } = 0; // For FamilyPermission and TimelineItemPermission only

        /// <summary>
        /// Gets or sets the unique identifier for the timeline item.
        /// </summary>
        public int ItemId { get; set; } = 0; // For TimelineItemPermission only

        /// <summary>
        /// Gets or sets the type of the timeline item.
        /// </summary>
        public KinaUnaTypes.TimeLineType? TimelineType { get; set; } = null; // For timeline items only

        /// <summary>
        /// Gets or sets the unique identifier for a permission.
        /// </summary>
        public int PermissionId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the type of permission associated with the entity (FamilyPermission, ProgenyPermission, TimelineItemPermission).
        /// </summary>
        /// <remarks>This property defines the category or scope of the permission. Ensure the value is 
        /// meaningful within the application's context and adheres to the specified length constraint.</remarks>
        [MaxLength(256)]
        public string PermissionType { get; set; } = string.Empty; // "FamilyPermission", "ProgenyPermission", "TimelineItemPermission"
        
        /// <summary>
        /// Gets or sets the action to be performed, such as "Add", "Update", or "Delete."
        /// </summary>
        public PermissionAction Action { get; set; } = PermissionAction.Unknown; // "Add", "Update", "Delete"
        
        /// <summary>
        /// Gets or sets the identifier of the user who made the change.
        /// </summary>
        [MaxLength(256)]
        public string ChangedBy { get; set; } = string.Empty; // UserId or email of the user who made the change

        /// <summary>
        /// The date and time when the change was made.
        /// </summary>
        public DateTime ChangeTime { get; set; }

        /// <summary>
        /// JSON or text details of the item before the change.
        /// </summary>
        [MaxLength(8192)]
        public string ItemBefore { get; set; } = string.Empty; // JSON or text details of the change

        /// <summary>
        /// JSON or text details of the item after the change.
        /// </summary>
        [MaxLength(8192)]
        public string ItemAfter { get; set; } = string.Empty; // JSON or text details of the change
    }
}
