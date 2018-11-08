using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class WebNotification
    {
        public int Id { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
        public string Icon { get; set; }
        public bool IsRead { get; set; }
        [NotMapped]
        public string DateTimeString { get; set; }
    }
}
