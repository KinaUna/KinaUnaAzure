using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUnaProgenyApi.Models
{
    public class Sleep
    {
        [Key]
        public int SleepId { get; set; }
        public int ProgenyId { get; set; }
        public DateTime SleepStart { get; set; }
        public DateTime SleepEnd { get; set; }
        public DateTime CreatedDate { get; set; }
        public int SleepRating { get; set; }
        public string SleepNotes { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }

        [NotMapped]
        public TimeSpan SleepDuration { get; set; }
    }
}
