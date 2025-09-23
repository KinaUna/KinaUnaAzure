using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Represents a permission granted for a specific resource, including details about the resource,  the user or
    /// group the permission is assigned to, and the level of access granted.
    /// </summary>
    /// <remarks>This class is used to define and manage permissions for resources in the system.  A resource
    /// can be associated with either a user or a group, but not both simultaneously.  The permission level determines
    /// the type of access granted to the resource.</remarks>
    public class ResourcePermission
    {
        /// <summary>
        /// Gets or sets the unique identifier for the resource permission.
        /// </summary>
        [Key]
        public int ResourcePermissionId { get; set; }

        /// <summary>
        /// Gets or sets the type of permission associated with the current operation.
        /// see <see cref="PermissionType"/> for details.
        /// </summary>
        public PermissionType PermissionType { get; set; } = PermissionType.TimelineItem;

        /// <summary>
        /// If PermissionType is TimelineItem, gets or sets the timeline type the resource permission is granted for.
        /// </summary>
        public KinaUnaTypes.TimeLineType? TimelineType { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the resource.
        /// </summary>
        public int ResourceId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the unique identifier of the user permission is granted for.
        /// Null or empty string if permission is granted to a group.
        /// </summary>
        [MaxLength(256)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the group permission is granted for.
        /// 0 if permission is granted to a user.
        /// </summary>
        public int GroupId { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the permission level associated with the permission granted.
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
