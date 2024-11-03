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
        public DateTime CreatedDate { get; set; }
        public int AccessLevel { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }

        [NotMapped]
        public int MeasurementNumber { get; set; }
    }
}
