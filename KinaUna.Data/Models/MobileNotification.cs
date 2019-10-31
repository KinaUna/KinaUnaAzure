using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    public class MobileNotification
    {
        [Key]
        public int NotificationId { get; set; }
        public string UserId { get; set; }
        public string ItemId { get; set; }
        public int ItemType { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string IconLink { get; set; }
        public DateTime Time { get; set; }
        public string Language { get; set; }
        public bool Read { get; set; }
    }
}
