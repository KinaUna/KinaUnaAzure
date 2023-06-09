﻿using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface IUserInfosHttpClient
    {
        /// <summary>
        /// Gets a user's information from the email address.
        /// </summary>
        /// <param name="email">string: The user's email address</param>
        /// <returns>UserInfo</returns>
        Task<UserInfo?> GetUserInfo(string email);

        /// <summary>
        /// Gets a user's information from the UserId.
        /// </summary>
        /// <param name="userId">string: The user's UserId (ApplicationUser.Id and UserInfo.UserId).</param>
        /// <returns>UserInfo</returns>
        Task<UserInfo?> GetUserInfoByUserId(string userId);

        /// <summary>
        /// Updates a UserInfo object. The UserInfo with the same Id will be updated.
        /// </summary>
        /// <param name="userinfo">UserInfo: The UserInfo object to update.</param>
        /// <returns>UserInfo: The updated UserInfo object.</returns>
        Task<UserInfo?> UpdateUserInfo(UserInfo userinfo);

        Task<UserInfo?> DeleteUserInfo(UserInfo userInfo);

        Task<List<UserInfo>?> GetDeletedUserInfos();

        Task<UserInfo?> RemoveUserInfoForGood(UserInfo userInfo);

        /// <summary>
        /// Sets the ViewChild for a given user.
        /// </summary>
        /// <param name="userId">string: The user's UserId (UserInfo.UserId or ApplicationUser.Id).</param>
        /// <param name="userinfo">UserInfo: The user's UserInfo.</param>
        /// <returns></returns>
        Task SetViewChild(string userId, UserInfo userinfo);
    }
}
