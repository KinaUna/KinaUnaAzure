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
        public string VaccinationName { get; set; }

        [MaxLength(4096)]
        public string VaccinationDescription { get; set; }
        public DateTime VaccinationDate { get; set; }
        public string Notes { get; set; }
        public int ProgenyId { get; set; }
        public int AccessLevel { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
    }
}
