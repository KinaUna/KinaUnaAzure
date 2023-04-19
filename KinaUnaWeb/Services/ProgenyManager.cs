using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    public class ProgenyManager : IProgenyManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdentityParser<ApplicationUser> _userManager;
        private readonly ImageStore _imageStore;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;

        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, IAuthHttpClient authHttpClient, IUserInfosHttpClient userInfosHttpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _authHttpClient = authHttpClient;
        }
        
        public async Task<UserInfo> GetInfo(string userEmail)
        {
            UserInfo userInfo = new();
            try
            {
                
                userInfo = await _userInfosHttpClient.GetUserInfo(userEmail);
                if (userInfo != null && !userInfo.IsKinaUnaUser)
                {
                    if (userInfo.UserEmail != "Unknown")
                    {
                        userInfo.IsKinaUnaUser = true;
                        _ = await UpdateUserInfo(userInfo);
                    }
                }
            }
            catch (Exception e)
            {
                if (userInfo != null)
                {
                    userInfo.UserId = "401";
                    userInfo.UserName = e.Message;
                    userInfo.UserEmail = Constants.DefaultUserEmail;
                    userInfo.CanUserAddItems = false;
                    userInfo.ViewChild = Constants.DefaultChildId;
                    return userInfo;
                }
            }

            if (userInfo != null && userInfo.UserEmail == "Unknown")
            {
                ApplicationUser applicationUser = _userManager.Parse(_httpContextAccessor.HttpContext?.User);
                
                UserInfo newUserinfo = new()
                {
                    UserEmail = applicationUser.Email,
                    ViewChild = 0,
                    UserId = applicationUser.Id,
                    FirstName = applicationUser.FirstName ?? "",
                    MiddleName = applicationUser.MiddleName ?? "",
                    LastName = applicationUser.LastName ?? "",
                    Timezone = applicationUser.TimeZone,
                    UserName = applicationUser.UserName,
                    IsKinaUnaUser = true
                };

                if (string.IsNullOrEmpty(newUserinfo.UserName))
                {
                    newUserinfo.UserName = newUserinfo.UserEmail;
                }

                userInfo = await _userInfosHttpClient.AddUserInfo(newUserinfo);
                
            }

            if (userInfo != null && userInfo.ViewChild == 0)
            {
                if (userInfo.ProgenyList.Any())
                {
                    await SetViewChild(userInfo.UserEmail, userInfo.ProgenyList[0].Id);
                }
                else
                {
                    userInfo.ViewChild = Constants.DefaultChildId;
                }
            }
            return userInfo;

        }

        public string GetImageUrl(string pictureLink, string pictureContainer)
        {
            string returnString = _imageStore.UriFor(pictureLink, pictureContainer);
            return returnString;
        }

        private async Task<UserInfo> UpdateUserInfo(UserInfo userInfo)
        {
            UserInfo updatedUserInfo = await _userInfosHttpClient.UpdateUserInfo(userInfo);
            return updatedUserInfo;
        }
        
        
        private async Task SetViewChild(string userEmail, int childId)
        {
            UserInfo currentUserInfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            currentUserInfo.ViewChild = childId;
            await _userInfosHttpClient.UpdateUserInfo(currentUserInfo);
        }

        public async Task<bool> IsUserLoginValid(string userId)
        {
            if (userId != Constants.DefaultUserId && userId != "401")
            {
                UserInfo userinfo = await _userInfosHttpClient.CheckCurrentUser(userId);
                if (userinfo?.UserId.ToUpper() == userId.ToUpper())
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> IsApplicationUserValid(string userId)
        {
            return await _authHttpClient.IsApplicationUserValid(userId);
        }
    }
}
