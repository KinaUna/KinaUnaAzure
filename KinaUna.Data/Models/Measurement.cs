using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Measurement data.
    /// </summary>
    public class Measurement
    {
        public int MeasurementId { get; set; }
        public int ProgenyId { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public double Circumference { get; set; }

        [MaxLength(256)]
        public string EyeColor { get; set; }

        [MaxLength(256)]
        public string HairColor { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedDate { get; set; } // Todo: Replace with CreatedTime?
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

        [NotMapped]
        public int MeasurementNumber { get; set; }
    }
}
