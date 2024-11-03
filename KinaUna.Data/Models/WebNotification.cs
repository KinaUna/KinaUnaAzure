using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for WebNotification data.
    /// </summary>
    public class WebNotification
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string To { get; set; }

        [MaxLength(256)]
        public string From { get; set; }

        [MaxLength(256)]
        public string Type { get; set; }

        [MaxLength(256)]
        public string Title { get; set; }

        [MaxLength(4096)]
        public string Message { get; set; }

        [MaxLength(1024)]
        public string Link { get; set; }
        public DateTime DateTime { get; set; }

        [MaxLength(1024)]
        public string Icon { get; set; }
        public bool IsRead { get; set; }
        [NotMapped]
        public string DateTimeString { get; set; }
    }
}
