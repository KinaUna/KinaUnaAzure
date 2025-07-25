using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing user data.
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="userManager"></param>
    /// <param name="authHttpClient"></param>
    /// <param name="userInfosHttpClient"></param>
    public class ProgenyManager(IHttpContextAccessor httpContextAccessor, IIdentityParser<ApplicationUser> userManager, IAuthHttpClient authHttpClient, IUserInfosHttpClient userInfosHttpClient)
        : IProgenyManager
    {
        /// <summary>
        /// Gets the UserInfo for a user with a given email.
        /// If the UserInfo is not found, a new UserInfo object is created and added to the database.
        /// If there is an error getting the UserInfo, a UserInfo object with UserId = "401" and UserName = the error message is returned, the user should be logged out in this case.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserInfo object.</returns>
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

        /// <summary>
        /// Updates the UserInfo object's ViewChild for the user with the given email in the database.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <param name="childId">The Id of the Progeny to set as currently viewed child.</param>
        /// <returns></returns>
        private async Task SetViewChild(string userEmail, int childId)
        {
            UserInfo currentUserInfo = await userInfosHttpClient.GetUserInfo(userEmail);
            currentUserInfo.ViewChild = childId;
            await userInfosHttpClient.UpdateUserInfo(currentUserInfo);
        }

        /// <summary>
        /// Checks if the user's UserInfo object is valid.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Boolean: True if the user data is valid.</returns>
        public async Task<bool> IsUserLoginValid(string userId)
        {
            if (userId == Constants.DefaultUserId || userId == "401") return false;

            UserInfo userinfo = await userInfosHttpClient.CheckCurrentUser(userId);
            return userinfo != null && (userinfo.UserId.ToUpper()).Equals(userId, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Checks if the user's ApplicationUser data in the IDP database is valid.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Boolean: True if the user data is valid.</returns>
        public async Task<bool> IsApplicationUserValid(string userId)
        {
            return await authHttpClient.IsApplicationUserValid(userId);
        }
    }
}
