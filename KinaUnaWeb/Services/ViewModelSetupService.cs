using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
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

        public async Task<BaseItemsViewModel> SetupViewModel(int languageId, string userEmail, int progenyId)
        {
           BaseItemsViewModel viewModel = new();
           string cachedBaseViewModel = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userEmail.ToUpper() + "_progeny_" + progenyId);
           if (!string.IsNullOrEmpty(cachedBaseViewModel))
           {
               viewModel = JsonConvert.DeserializeObject<BaseItemsViewModel>(cachedBaseViewModel);
               return viewModel;
           }

           viewModel.LanguageId = languageId;
           viewModel.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
           viewModel.SetCurrentProgenyId(progenyId);
           viewModel.CurrentProgeny = await _progenyHttpClient.GetProgeny(viewModel.CurrentProgenyId);
           viewModel.CurrentProgenyAccessList = await _userAccessHttpClient.GetProgenyAccessList(viewModel.CurrentProgenyId);
           viewModel.SetCurrentUsersAccessLevel();

           await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userEmail.ToUpper() + "_progeny_" + progenyId, JsonConvert.SerializeObject(viewModel), _cacheOptions);
           
           return viewModel;
        }
        
        public async Task<List<SelectListItem>> GetProgenySelectList(UserInfo userInfo)
        {
            List<SelectListItem> progenyList = new();
            List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userInfo.UserEmail);
            if (accessList.Any())
            {
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
            }

            return progenyList;
        }

        public async Task<TimeLineViewModel> GetLatestPostTimeLineModel(int progenyId, int accessLevel, int languageId)
        {
            TimeLineViewModel latestPosts = new()
            {
                LanguageId = languageId,
                TimeLineItems = await _progenyHttpClient.GetProgenyLatestPosts(progenyId, accessLevel)
            };
            if (latestPosts.TimeLineItems.Any())
            {
                latestPosts.TimeLineItems = latestPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).Take(5).ToList();
            }

            return latestPosts;
        }

        public async Task<TimeLineViewModel> GetYearAgoPostsTimeLineModel(int progenyId, int accessLevel, int languageId)
        {
            TimeLineViewModel yearAgoPosts = new()
            {
                LanguageId = languageId,
                TimeLineItems = await _progenyHttpClient.GetProgenyYearAgo(progenyId, accessLevel)
            };
            if (yearAgoPosts.TimeLineItems.Any())
            {
                yearAgoPosts.TimeLineItems = yearAgoPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).ToList();
            }

            return yearAgoPosts;
        }
    }
}
