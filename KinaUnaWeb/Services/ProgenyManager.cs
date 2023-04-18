﻿using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IAuthHttpClient _authHttpClient;
        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, HttpClient httpClient, ApiTokenInMemoryClient apiTokenClient, IAuthHttpClient authHttpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
            _authHttpClient = authHttpClient;
        }
        
        public async Task<UserInfo> GetInfo(string userEmail)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string userInfoApiPath = "/api/UserInfo/ByEmail/" + userEmail;
            UserInfo userInfo = new();
            try
            {
                string userInfoResponseString = await _httpClient.GetStringAsync(userInfoApiPath);
                userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseString);
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

                string newUserinfoApiPath = "/api/UserInfo/";
                HttpResponseMessage newUserResponse = await _httpClient.PostAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(newUserinfo), System.Text.Encoding.UTF8, "application/json"));
                if (newUserResponse.IsSuccessStatusCode)
                {
                    string newUserResponseString = await newUserResponse.Content.ReadAsStringAsync();
                    userInfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
                }
                
            }

            if (userInfo != null && userInfo.ViewChild == 0)
            {
                if (userInfo.ProgenyList.Any())
                {
                    await SetViewChild(userInfo.UserEmail, userInfo.ProgenyList[0].Id, userInfo.UserId);
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newUserinfoApiPath = "/api/UserInfo/" + userInfo.UserId;
            HttpResponseMessage userInfoResponse = await _httpClient.PutAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (userInfoResponse.IsSuccessStatusCode)
            {
                string userInfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
                UserInfo updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(userInfoAsString);
                return updatedUserinfo;
            }

            return new UserInfo();
        }
        
        
        private async Task SetViewChild(string userEmail, int childId, string userId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            UserInfo userinfo = new()
            {
                UserEmail = userEmail,
                ViewChild = childId,
                UserId = userId
            };

            string setChildApiPath = "/api/UserInfo/" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }

        public async Task<bool> IsUserLoginValid(string userId)
        {
            if (userId != Constants.DefaultUserId && userId != "401")
            {
                HttpContext currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        _httpClient.SetBearerToken(contextAccessToken);
                        const string userinfoApiPath = "/api/UserInfo/CheckCurrentUser/";
                        HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userinfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
                        if (userInfoResponse.IsSuccessStatusCode)
                        {
                            string userInfoResponseAsString = await userInfoResponse.Content.ReadAsStringAsync();
                            UserInfo userinfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseAsString);
                            if (userinfo?.UserId.ToUpper() == userId.ToUpper())
                            {
                                return true;
                            }
                        }
                    }
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
