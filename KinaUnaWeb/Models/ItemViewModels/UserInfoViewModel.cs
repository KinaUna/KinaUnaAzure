using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class UserInfoViewModel: BaseViewModel
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
            ReadOnlyCollection<TimeZoneInfo> tzs = TimeZoneInfo.GetSystemTimeZones();
            TimezoneList = tzs.Select(tz => new SelectListItem()
            {
                Text = tz.DisplayName,
                Value = tz.Id
            }).ToArray();
        }
    }

    public static class UserInfoViewModelExtensions
    {
        public static void CopyPropertiesFromUserInfoViewModel(this UserInfo userInfo, UserInfoViewModel viewModel)
        {
            userInfo.FirstName = viewModel.FirstName;
            userInfo.MiddleName = viewModel.MiddleName;
            userInfo.LastName = viewModel.LastName;

            userInfo.UserName = viewModel.UserName;
            if (string.IsNullOrEmpty(userInfo.UserName))
            {
                userInfo.UserName = userInfo.UserEmail;
            }

            userInfo.Timezone = viewModel.Timezone;

            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }
        }
    }
}
