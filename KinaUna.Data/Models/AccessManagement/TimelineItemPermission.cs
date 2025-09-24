using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    public class TimelineItemPermission
    {
        /// <summary>
        /// The unique identifier for the timeline item permission.
        /// </summary>
        [Key]
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
        /// </summary>
        public int ProgenyId { get; set; } // The ID of the progeny the timeline item belongs to. 0 if FamilyId is set.

        public int FamilyId { get; set; } // The ID of the family the timeline item belongs to. 0 if ProgenyId is set.
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [MaxLength(256)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty; // Fallback for UserId, in case we want to invite a user by email.

        /// <summary>
        /// Gets or sets the group identifier if the permission is granted to a group.
        /// </summary>
        public int GroupId { get; set; } = 0; // GroupId is 0 if permission is granted to a user.

        /// <summary>
        /// Gets or sets the permission level assigned to the user or entity.
        /// </summary>
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;

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
