using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWebBlazor.Models.FamilyViewModels
{
    public class ProgenyViewModel: BaseViewModel
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
        public IFormFile? File { get; set; }
        public SelectListItem[] TimezoneList { get; set; }

        public ProgenyViewModel()
        {
            Name = "";
            NickName = "";
            TimeZone = "";
            PictureLink = "";
            Admins = "";
            ReadOnlyCollection<TimeZoneInfo> tzs = TimeZoneInfo.GetSystemTimeZones();
            TimezoneList = tzs.Select(tz => new SelectListItem()
            {
                Text = tz.DisplayName,
                Value = tz.Id
            }).ToArray();
        }
    }
}
