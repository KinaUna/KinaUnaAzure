using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Progeny data.
    /// </summary>
    public class Progeny
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(256)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string NickName { get; set; }
        public DateTime? BirthDay { get; set; }

        [MaxLength(256)]
        public string TimeZone { get; set; }

        [MaxLength(512)]
        public string PictureLink { get; set; }

        [MaxLength(1024)]
        public string Admins { get; set; } // Comma separated list of emails.

    }
}
