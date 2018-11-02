using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class ProgenyViewModel
    {
        public int ProgenyId { get; set; }
        [Display(Name = "Full Name")]
        public string Name { get; set; }
        [Display(Name = "Display Name")]
        public string NickName { get; set; }
        public DateTime? BirthDay { get; set; }
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; }
        public string PictureLink { get; set; }
        [Display(Name = "Administrators")]
        public string Admins { get; set; } // Comma separated list of emails.
        public SelectListItem[] TimezoneList { get; set; }

        public ProgenyViewModel()
        {
            var tzs = TimeZoneInfo.GetSystemTimeZones();
            TimezoneList = tzs.Select(tz => new SelectListItem()
            {
                Text = tz.DisplayName,
                Value = tz.Id
            }).ToArray();
        }
    }
}
