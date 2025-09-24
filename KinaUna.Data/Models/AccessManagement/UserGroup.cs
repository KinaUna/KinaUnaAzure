using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Represents a group of users, including metadata such as ownership, creation details, and group attributes.
    /// </summary>
    /// <remarks>A <see cref="UserGroup"/> can represent various types of user collections, such as family
    /// groups or other organizational units. It includes properties for identifying the group, tracking ownership, and
    /// storing descriptive information. The group can also include a list of members, which is not persisted in the
    /// database table.</remarks>
    public class UserGroup
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user group.
        /// </summary>
        [Key]
        public int UserGroupId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is marked as a family group.
        /// </summary>
        public bool IsFamily { get; set; } = false;

        /// <summary>
        /// Gets or sets the name associated with the group.
        /// </summary>
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the group.
        /// </summary>
        [MaxLength(4096)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for the associated progeny, if applicable.
        /// </summary>
        public int ProgenyId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the unique identifier for the associated family, if applicable.
        /// </summary>
        public int FamilyId { get; set; } = 0;

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

        [NotMapped]
        public List<UserGroupMember> Members { get; set; } = [];
    }
}
