using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Represents a permission assigned to a user for a specific family entity.
    /// </summary>
    /// <remarks>This class is used to manage and track user permissions within the context of a family
    /// entity.  Permissions are associated with a user, identified by their user ID or email, and specify the  level of
    /// access granted. The class also includes metadata for auditing purposes, such as creation  and modification
    /// timestamps and the identifiers of the users or systems responsible for those actions.</remarks>
    public class FamilyPermission
    {
        /// <summary>
        /// Gets or sets the unique identifier for the family permission.
        /// </summary>
        [Key]
        public int FamilyPermissionId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the family.
        /// </summary>
        public int FamilyId { get; set; }

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
