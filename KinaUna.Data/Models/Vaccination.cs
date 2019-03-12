using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    public class Vaccination
    {
        [Key]
        public int VaccinationId { get; set; }
        public string VaccinationName { get; set; }
        public string VaccinationDescription { get; set; }
        public DateTime VaccinationDate { get; set; }
        public string Notes { get; set; }
        public int ProgenyId { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
    }
}
