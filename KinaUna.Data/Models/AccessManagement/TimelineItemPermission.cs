using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models.AccessManagement
{
    public class TimelineItemPermission
    {
        /// <summary>
        /// The unique identifier for the timeline item permission.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TimelineItemPermissionId { get; set; }

        /// <summary>
        /// The type of timeline item the permission is associated with.
        /// </summary>
        public KinaUnaTypes.TimeLineType TimelineType { get; set; } // The type of timeline the item belongs to (e.g., Note, TodoItem, Sleep, etc.).

        /// <summary>
        /// The unique identifier for the specific item for the given type.
        /// </summary>
        public int ItemId { get; set; } // The ID of the item in the timeline (e.g., the specific event or entry).

        /// <summary>
        /// The unique identifier for the progeny the timeline item belongs to.
        /// 0 if FamilyId is set.
        /// </summary>
        public int ProgenyId { get; set; }

        /// <summary>
        /// The unique identifier for the family the timeline item belongs to.
        /// 0 if ProgenyId is set.
        /// </summary>
        public int FamilyId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// Empty string if permission is granted to a group.
        /// </summary>
        [MaxLength(256)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address associated with the user. Empty string if permission is granted to a group.
        /// Only used as a fallback for UserId, in case we want to invite a user by email.
        /// </summary>
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group identifier if the permission is granted to a group.
        /// 0 if permission is granted to a user.
        /// </summary>
        public int GroupId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the permission level assigned to the user or entity.
        /// </summary>
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;

        /// <summary>
        /// Indicates whether the item inherits permissions from a parent entity (e.g., family or progeny).
        /// </summary>
        public bool InheritPermissions { get; set; } = true;

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user or system that created the entity.
        /// </summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the user or system that last modified the entity.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
