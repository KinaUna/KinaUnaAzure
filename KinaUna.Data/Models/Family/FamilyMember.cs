using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models.Family
{
    /// <summary>
    /// Represents a member of a family, including their role, associated identifiers, and metadata.
    /// </summary>
    /// <remarks>This class is used to model a family member within a family structure. It includes properties
    /// for identifying the family member, their role within the family, and additional metadata such as creation and
    /// modification details. The class supports associating a family member with a specific user, email, or progeny.
    /// </remarks>
    public class FamilyMember
    {
        /// <summary>
        /// Gets or sets the unique identifier for a family member.
        /// </summary>
        public int FamilyMemberId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the family this member belongs to.
        /// </summary>
        public int FamilyId { get; set; }

        /// <summary>
        /// Gets or sets the type of family member (e.g. Parent, Child, Pet). Defaults to Unknown.
        /// </summary>
        public FamilyMemberType MemberType { get; set; } = FamilyMemberType.Unknown;

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// <remarks>It may be an empty string if we only have an email.</remarks>
        /// </summary>
        [MaxLength(256)]
        public string UserId { get; set; } = string.Empty; // UserId is empty string if we only have an email.

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// <remarks>For fallback when we don't have a UserId. As soon as a user with the email signs up the UserId property
        /// should be updated.</remarks>
        /// </summary>
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty; // Fallback for UserId, in case we want to invite a user by email.
        
        /// <summary>
        /// Gets or sets the unique identifier for the associated progeny, if it exists.
        /// </summary>
        public int ProgenyId { get; set; } = 0; // ProgenyId is 0 if the member is not associated with a specific progeny.
        
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
        /// Gets or sets the permission level associated with the entity.
        /// </summary>
        [NotMapped]
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;

        /// <summary>
        /// Gets or sets the associated progeny for the current entity.
        /// </summary>
        [NotMapped]
        public Progeny Progeny { get; set; } = new Progeny();
    }
}
