using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    }
}
