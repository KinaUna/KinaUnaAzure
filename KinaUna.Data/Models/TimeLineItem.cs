using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    public class TimeLineItem
    {
        [Key]
        public int TimeLineId { get; set; }
        public int ProgenyId { get; set; }
        public DateTime ProgenyTime { get; set; }
        public DateTime CreatedTime { get; set; }
        public int ItemType { get; set; }
        public string ItemId { get; set; }
        public string CreatedBy { get; set; }
        public int AccessLevel { get; set; }
    }
}
