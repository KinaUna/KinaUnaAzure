using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Represents a member of a user group, including details about the user, group ownership, and audit metadata.
    /// </summary>
    /// <remarks>This class is used to model the relationship between a user and a user group, including
    /// ownership details and audit information such as creation and modification timestamps. It supports both user
    /// identification via a unique user ID and fallback identification via email.</remarks>
    public class UserGroupMember
    {
        /// <summary>
        /// Gets or sets the unique identifier for a user group member.
        /// </summary>
        [Key]
        public int UserGroupMemberId { get; set; }

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        [MaxLength(256)]
        public string Email { get; set; } = ""; // Fallback for UserId, in case we want to invite a user by email.

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [MaxLength(256)]
        public string UserId { get; set; } = ""; // UserId is empty string if we only have an email.

        /// <summary>
        /// The ID of the UserGroup this member belongs to.
        /// </summary>
        public int UserGroupId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user who owns the resource.
        /// </summary>
        [MaxLength(256)]
        public string UserOwnerUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the family owner identifier associated with the group.
        /// Set to 0 if only one user is granted ownership of the resource.
        /// </summary>
        public int FamilyOwnerId { get; set; } = 0;

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
        
        /// <summary>
        /// Gets or sets the associated progeny for the current entity.
        /// </summary>
        /// <remarks>This property is not mapped to the database and is intended for use in application
        /// logic only.</remarks>
        [NotMapped]
        public Progeny Progeny { get; set; } = new Progeny();

        /// <summary>
        /// Gets or sets the user information associated with this entity.
        /// </summary>
        /// <remarks>This property is not mapped to the database and is intended for use in application
        /// logic only.</remarks>
        [NotMapped]
        public UserInfo UserInfo { get; set; } = new UserInfo();
    }
}
