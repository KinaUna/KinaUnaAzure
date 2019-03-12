using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    public class WebNotification
    {
        public int Id { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public DateTime DateTime { get; set; }
        public string Icon { get; set; }
        public bool IsRead { get; set; }
        [NotMapped]
        public string DateTimeString { get; set; }
    }
}
