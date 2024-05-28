using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class UserInfoViewModel: BaseViewModel
    {
        public int Id { get; set; } = 0;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int ViewChild { get; set; } = 0;
        public string Timezone { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
        [NotMapped] public string PhoneNumber { get; set; } = string.Empty;
        [NotMapped] public bool IsEmailConfirmed { get; set; } = false;
        [NotMapped] public string JoinDate { get; set; } = string.Empty;
        [NotMapped] public List<Progeny> ProgenyList { get; set; } = [];
        [NotMapped] public bool CanUserAddItems { get; set; } = false;
        [NotMapped] public List<UserAccess> AccessList { get; set; } = [];
        [NotMapped] public SelectListItem[] TimezoneList { get; set; }
        [NotMapped] public string ChangeLink { get; set; } = string.Empty;
        public UserInfoViewModel()
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
