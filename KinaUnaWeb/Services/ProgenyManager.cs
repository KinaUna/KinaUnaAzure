using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    public class ProgenyManager(IHttpContextAccessor httpContextAccessor, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, IAuthHttpClient authHttpClient, IUserInfosHttpClient userInfosHttpClient)
        : IProgenyManager
    {
        public async Task<UserInfo> GetInfo(string userEmail)
        {
            UserInfo userInfo = new();
            try
            {
                userInfo = await userInfosHttpClient.GetUserInfo(userEmail);
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
                ApplicationUser applicationUser = userManager.Parse(httpContextAccessor.HttpContext?.User);
                
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
                };

                if (string.IsNullOrEmpty(newUserinfo.UserName))
                {
                    newUserinfo.UserName = newUserinfo.UserEmail;
                }

                userInfo = await userInfosHttpClient.AddUserInfo(newUserinfo);
                
            }

            if (userInfo == null || userInfo.ViewChild != 0) return userInfo;

            if (userInfo.ProgenyList.Count != 0)
            {
                await SetViewChild(userInfo.UserEmail, userInfo.ProgenyList[0].Id);
            }
            else
            {
                userInfo.ViewChild = Constants.DefaultChildId;
            }
            return userInfo;

        }

        public string GetImageUrl(string pictureLink, string pictureContainer)
        {
            string returnString = imageStore.UriFor(pictureLink, pictureContainer);
            return returnString;
        }
        
        private async Task SetViewChild(string userEmail, int childId)
        {
            UserInfo currentUserInfo = await userInfosHttpClient.GetUserInfo(userEmail);
            currentUserInfo.ViewChild = childId;
            await userInfosHttpClient.UpdateUserInfo(currentUserInfo);
        }

        public async Task<bool> IsUserLoginValid(string userId)
        {
            if (userId == Constants.DefaultUserId || userId == "401") return false;

            UserInfo userinfo = await userInfosHttpClient.CheckCurrentUser(userId);
            return userinfo != null && (userinfo.UserId.ToUpper()).Equals(userId, StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task<bool> IsApplicationUserValid(string userId)
        {
            return await authHttpClient.IsApplicationUserValid(userId);
        }
    }
}
