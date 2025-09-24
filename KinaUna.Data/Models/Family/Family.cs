using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models.Family
{
    /// <summary>
    /// Represents a family entity, including its metadata and administrative details.
    /// </summary>
    /// <remarks>The <see cref="Family"/> class is designed to store information about a family, including its
    /// name,  description, administrators, and metadata such as creation and modification details.  The <see
    /// cref="Admins"/> property contains a comma-separated list of email addresses for administrators.</remarks>
    public class Family
    {
        /// <summary>
        /// Gets or sets the unique identifier for the family.
        /// </summary>
        public int FamilyId { get; set; }

        /// <summary>
        /// The name associated with the family.
        /// </summary>
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description text. The maximum length is 4,096 characters.
        /// </summary>
        [MaxLength(4096)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a comma-separated list of administrator email addresses.
        /// </summary>
        /// <remarks>Users with a matching email address should be granted admin access rights.
        /// Ensure that each email address in the list is valid and properly formatted. 
        /// Exceeding the maximum length of 4096 characters will result in a validation error.</remarks>
        [MaxLength(4096)]
        public string Admins { get; set; } // Comma separated list of emails

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
        public List<FamilyMember> FamilyMembers { get; set; } = [];
    }
}
