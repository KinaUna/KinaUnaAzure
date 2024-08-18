using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for setting up the BaseItemsViewModel and related entities.
    /// Gets the frequently used ViewModel properties, such as LanguageId, CurrentUser, CurrentProgeny, CurrentProgenyAccessList and CurrentUsersAccessLevel properties.
    /// </summary>
    public class ViewModelSetupService : IViewModelSetupService
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        
        public ViewModelSetupService(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient,IDistributedCache cache)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 0, 30)); // Expire after 30 seconds.
        }

        /// <summary>
        /// Sets up the BaseItemsViewModel for the given user and Progeny and language.
        /// First checks the cache for a cached ViewModel, if not found, generates a new one via API calls.
        /// </summary>
        /// <param name="languageId">The language Id set for the current user.</param>
        /// <param name="userEmail">The user's email address.</param>
        /// <param name="progenyId">The ProgenyId for the Progeny.</param>
        /// <returns>BaseItemsViewModel</returns>
        public async Task<BaseItemsViewModel> SetupViewModel(int languageId, string userEmail, int progenyId)
        {
            BaseItemsViewModel viewModel = new()
            {
                CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail)
            };

            if (progenyId == 0)
            {
               
                progenyId = viewModel.CurrentUser.ViewChild;
            }
            else
            {
                viewModel.CurrentUser.ViewChild = progenyId;
            }
            
            string cachedBaseViewModel = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userEmail.ToUpper() + "_progeny_" + progenyId);
            if (!string.IsNullOrEmpty(cachedBaseViewModel))
            {
                viewModel = JsonConvert.DeserializeObject<BaseItemsViewModel>(cachedBaseViewModel);
                return viewModel;
            }

            viewModel.LanguageId = languageId;
           
            viewModel.SetCurrentProgenyId(progenyId);
            viewModel.CurrentProgeny = await _progenyHttpClient.GetProgeny(viewModel.CurrentProgenyId);
            viewModel.CurrentProgenyAccessList = await _userAccessHttpClient.GetProgenyAccessList(viewModel.CurrentProgenyId);
            viewModel.SetCurrentUsersAccessLevel();

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userEmail.ToUpper() + "_progeny_" + progenyId, JsonConvert.SerializeObject(viewModel), _cacheOptions);
           
            return viewModel;
        }

        /// <summary>
        /// Generates a SelectListItem list of Progeny for the given user.
        /// </summary>
        /// <param name="userInfo">The user's UserInfo data.</param>
        /// <returns>List of SelectListItem objects.</returns>
        public async Task<List<SelectListItem>> GetProgenySelectList(UserInfo userInfo)
        {
            List<SelectListItem> progenyList = [];
            List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userInfo.UserEmail);
            if (accessList.Count == 0) return progenyList;

            foreach (Progeny progeny in accessList)
            {
                SelectListItem selItem = new()
                {
                    Text = accessList.Single(p => p.Id == progeny.Id).NickName,
                    Value = progeny.Id.ToString()
                };
                if (progeny.Id == userInfo.ViewChild)
                {
                    selItem.Selected = true;
                }

                progenyList.Add(selItem);
            }

            return progenyList;
        }
    }
}
