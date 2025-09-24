using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Represents a permission assigned to a user or entity for accessing or managing a specific progeny.
    /// </summary>
    /// <remarks>This class is used to define and manage access control for progeny-related data. Each
    /// instance associates a user or entity with a specific progeny and specifies the level of access
    /// granted.</remarks>
    public class ProgenyPermission
    {
        /// <summary>
        /// The unique identifier for the progeny permission.
        /// </summary>
        [Key]
        public int ProgenyPermissionId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the progeny.
        /// </summary>
        public int ProgenyId { get; set; }

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
