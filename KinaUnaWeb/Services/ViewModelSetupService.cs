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
        private readonly ITranslationsHttpClient _translationsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        
        public ViewModelSetupService(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient,
            ITranslationsHttpClient translationsHttpClient, IUserAccessHttpClient userAccessHttpClient,IDistributedCache cache)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _translationsHttpClient = translationsHttpClient;
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
        /// <param name="selectedProgenyId">The Id of the Progeny to select in the list. If 0, the current user's default progeny is selected.</param>
        /// <returns>List of SelectListItem objects.</returns>
        public async Task<List<SelectListItem>> GetProgenySelectList(UserInfo userInfo, int selectedProgenyId = 0)
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
                if (selectedProgenyId == 0)
                {
                    if (progeny.Id == userInfo.ViewChild)
                    {
                        selItem.Selected = true;
                    }
                }
                else
                {
                    if (progeny.Id == selectedProgenyId)
                    {
                        selItem.Selected = true;
                    }
                }

                progenyList.Add(selItem);
            }

            return progenyList;
        }

        /// <summary>
        /// Generates a SelectListItem list of offset times for setting reminders.
        /// </summary>
        /// <param name="languageId">The user's language.</param>
        /// <returns>List of SelectListItems.</returns>
        public async Task<List<SelectListItem>> CreateReminderOffsetSelectListItems(int languageId)
        {
            List<SelectListItem> reminderOffsetList = new();
            string minutesBeforeText = await _translationsHttpClient.GetTranslation("minutes before", PageNames.Calendar, languageId);
            string hourBeforeText = await _translationsHttpClient.GetTranslation("hour before", PageNames.Calendar, languageId);
            string dayBeforeText = await _translationsHttpClient.GetTranslation("day before", PageNames.Calendar, languageId);
            string customText = await _translationsHttpClient.GetTranslation("Custom...", PageNames.Calendar, languageId);

            SelectListItem reminderOffset5Min = new SelectListItem { Value = "5", Text = $"5 {minutesBeforeText}" };
            SelectListItem reminderOffset10Min = new SelectListItem { Value = "10", Text = $"10 {minutesBeforeText}" };
            SelectListItem reminderOffset15Min = new SelectListItem { Value = "15", Text = $"15 {minutesBeforeText}" };
            SelectListItem reminderOffset20Min = new SelectListItem { Value = "20", Text = $"20 {minutesBeforeText}" };
            SelectListItem reminderOffset30Min = new SelectListItem { Value = "30", Text = $"30 {minutesBeforeText}", Selected = true};
            SelectListItem reminderOffset1Hour = new SelectListItem { Value = "60", Text = $"1 {hourBeforeText}" };
            SelectListItem reminderOffset1Day = new SelectListItem { Value = "1440", Text = $"1 {dayBeforeText}" };
            SelectListItem reminderOffsetCustom = new SelectListItem { Value = "0", Text = customText };

            reminderOffsetList.Add(reminderOffset5Min);
            reminderOffsetList.Add(reminderOffset10Min);
            reminderOffsetList.Add(reminderOffset15Min);
            reminderOffsetList.Add(reminderOffset20Min);
            reminderOffsetList.Add(reminderOffset30Min);
            reminderOffsetList.Add(reminderOffset1Hour);
            reminderOffsetList.Add(reminderOffset1Day);
            reminderOffsetList.Add(reminderOffsetCustom);

            return reminderOffsetList;
        }
    }
}
