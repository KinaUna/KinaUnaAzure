﻿using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface IProgenyManager
    {
        Task<UserInfo> GetInfo(string userEmail);
        string GetImageUrl(string pictureLink, string pictureContainer);
        Task<bool> IsApplicationUserValid(string userId);
        Task<bool> IsUserLoginValid(string userId);
    }
}
