using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class Progeny
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string NickName { get; set; }
        public DateTime? BirthDay { get; set; }
        public string TimeZone { get; set; }
        public string PictureLink { get; set; }
        public string Admins { get; set; } // Comma separated list of emails.

    }
}
