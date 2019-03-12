using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UserInfoViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public int ViewChild { get; set; }
        public string Timezone { get; set; }
        public string ProfilePicture { get; set; }
        public IFormFile File { get; set; }
        [NotMapped]
        public string PhoneNumber { get; set; }
        [NotMapped]
        public bool IsEmailConfirmed { get; set; }
        [NotMapped]
        public string JoinDate { get; set; }
        [NotMapped]
        public List<Progeny> ProgenyList { get; set; }
        [NotMapped]
        public bool CanUserAddItems { get; set; }
        [NotMapped]
        public List<UserAccess> AccessList { get; set; }
        [NotMapped]
        public SelectListItem[] TimezoneList { get; set; }
        [NotMapped]
        public string ChangeLink { get; set; }
        public UserInfoViewModel()
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
