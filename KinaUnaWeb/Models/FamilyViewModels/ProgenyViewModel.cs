using System;
using System.Collections.ObjectModel;
using System.Linq;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class ProgenyViewModel: BaseViewModel
    {
        public int ProgenyId { get; set; }
        public string Name { get; set; }
        public string NickName { get; set; }
        public DateTime? BirthDay { get; set; }
        public string TimeZone { get; set; }
        public string PictureLink { get; set; }
        public string Admins { get; set; } // Comma separated list of emails.
        public IFormFile File { get; init; }
        public SelectListItem[] TimezoneList { get; init; }

        public ProgenyInfo ProgenyInfo { get; set; }

        public ProgenyViewModel()
        {
            ReadOnlyCollection<TimeZoneInfo> tzs = TimeZoneInfo.GetSystemTimeZones();
            TimezoneList = tzs.Select(tz => new SelectListItem()
            {
                Text = tz.DisplayName,
                Value = tz.Id
            }).ToArray();
        }
    }
}
