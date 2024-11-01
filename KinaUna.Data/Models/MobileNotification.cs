using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for MobileNotification data.
    /// </summary>
    public class MobileNotification
    {
        [Key]
        public int NotificationId { get; init; }

        [MaxLength(256)]
        public string UserId { get; set; }

        [MaxLength(128)]
        public string ItemId { get; set; }
        public int ItemType { get; set; }

        [MaxLength(256)]
        public string Title { get; set; }

        [MaxLength(4096)]
        public string Message { get; set; }

        [MaxLength(512)]
        public string IconLink { get; set; }
        public DateTime Time { get; set; }

        [MaxLength(256)]
        public string Language { get; set; }
        public bool Read { get; set; }
    }
}
