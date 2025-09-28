using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Vaccination data.
    /// </summary>
    public class Vaccination
    {
        [Key]
        public int VaccinationId { get; set; }

        [MaxLength(256)]
        public string VaccinationName { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string VaccinationDescription { get; set; } = string.Empty;
        public DateTime VaccinationDate { get; set; }
        [MaxLength(4096)]
        public string Notes { get; set; } = string.Empty;
        public int ProgenyId { get; set; }
        public int AccessLevel { get; set; }

        [MaxLength(256)]
        public string Author { get; set; } = string.Empty; // Todo: Replace with CreatedBy?

        /// <summary>
        /// Gets or sets the identifier of the user or system that created the entity.
        /// </summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user or system that last modified the entity.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
    }
}
