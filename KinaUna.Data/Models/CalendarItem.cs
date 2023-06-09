﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    public class CalendarItem
    {
        [Key]
        public int EventId { get; set; }
        public int ProgenyId { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; }
        public string Context { get; set; }
        public bool AllDay { get; set; }
        public int AccessLevel { get; set; }

        public string StartString { get; set; }
        public string EndString { get; set; }
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
        [NotMapped]
        public DateTime Start { get; set; }
        [NotMapped]
        public DateTime End { get; set; }
        [NotMapped]
        public bool IsReadonly { get; set; }
    }
}
