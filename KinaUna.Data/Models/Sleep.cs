using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Sleep data.
    /// </summary>
    public class Sleep
    {
        [Key]
        public int SleepId { get; set; }
        public int ProgenyId { get; set; }
        public DateTime SleepStart { get; set; }
        public DateTime SleepEnd { get; set; }
        public DateTime CreatedDate { get; set; }
        public int SleepRating { get; set; }

        [MaxLength(4096)]
        public string SleepNotes { get; set; }
        public int AccessLevel { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }

        [NotMapped]
        public TimeSpan SleepDuration { get; set; }
        [NotMapped]
        public string StartString { get; set; }
        [NotMapped]
        public string EndString { get; set; }
        [NotMapped]
        public Progeny Progeny { get; set; }

        [NotMapped]
        public int SleepNumber { get; set; }
    }
}
